#!/usr/bin/env node
/**
 * FluentConfig cold/warm open timing harness.
 *
 * Connects to Streamer.bot Client WebSocket, DoAction by name, then polls
 * GetGlobal for persisted FluentConfig_PerfLast until the value changes.
 *
 * Note: Streamer.bot 1.0.4 returns GetGlobal under `variables.<name>`; the harness
 * also accepts the singular `variable` shape documented elsewhere.
 *
 * Env:
 *   STREAMERBOT_WS_URL       default ws://127.0.0.1:8080/
 *   STREAMERBOT_WS_PASSWORD  optional; required when SB Enforce auth is on
 *   PERF_ACTION_NAME         default "FluentConfig Perf Benchmark"
 *   PERF_RUNS                default 2 (1 cold + 1 warm)
 *   PERF_POLL_MS             default 250
 *   PERF_TIMEOUT_MS          default 60000
 *   PERF_PAUSE_MS            default 0; if >0, auto-wait between runs instead of Enter
 */

import { createInterface } from 'node:readline';
import { createHash, randomUUID } from 'node:crypto';
import WebSocket from 'ws';

const WS_URL = process.env.STREAMERBOT_WS_URL || 'ws://127.0.0.1:8080/';
const WS_PASSWORD = process.env.STREAMERBOT_WS_PASSWORD || '';
const ACTION_NAME = process.env.PERF_ACTION_NAME || 'FluentConfig Perf Benchmark';
const GLOBAL_NAME = 'FluentConfig_PerfLast';
const RUNS = Math.max(1, Number.parseInt(process.env.PERF_RUNS || '2', 10) || 2);
const POLL_MS = Math.max(50, Number.parseInt(process.env.PERF_POLL_MS || '250', 10) || 250);
const TIMEOUT_MS = Math.max(1000, Number.parseInt(process.env.PERF_TIMEOUT_MS || '60000', 10) || 60000);
const PAUSE_MS = Math.max(0, Number.parseInt(process.env.PERF_PAUSE_MS || '0', 10) || 0);

/** @typedef {{ cold: boolean, totalMs: number, milestones: Array<{ name: string, ms: number, kind: string }> }} PerfLast */

class StreamerBotClient {
  /**
   * @param {string} url
   * @param {string} [password]
   */
  constructor(url, password = '') {
    this.url = url;
    this.password = password;
    /** @type {WebSocket | null} */
    this.ws = null;
    /** @type {Map<string, { resolve: (v: any) => void, reject: (e: Error) => void }>} */
    this.pending = new Map();
    /** @type {((msg: any) => void) | null} */
    this._helloResolve = null;
    /** @type {((err: Error) => void) | null} */
    this._helloReject = null;
  }

  connect() {
    return new Promise((resolve, reject) => {
      const ws = new WebSocket(this.url);
      this.ws = ws;

      const helloPromise = new Promise((hRes, hRej) => {
        this._helloResolve = hRes;
        this._helloReject = hRej;
      });

      ws.on('open', () => {
        /* wait for Hello before resolving connect */
      });

      ws.on('message', (data) => {
        let msg;
        try {
          msg = JSON.parse(String(data));
        } catch {
          return;
        }

        if (msg.request === 'Hello') {
          this._helloResolve?.(msg);
          this._helloResolve = null;
          this._helloReject = null;
          return;
        }

        const id = msg.id;
        if (id != null && this.pending.has(id)) {
          const { resolve: res, reject: rej } = this.pending.get(id);
          this.pending.delete(id);
          if (msg.status === 'error') {
            rej(new Error(msg.error ?? msg.message ?? JSON.stringify(msg)));
          } else {
            res(msg);
          }
        }
      });

      ws.on('error', (err) => {
        if (this._helloReject) {
          this._helloReject(err);
          this._helloResolve = null;
          this._helloReject = null;
        }
        for (const [, p] of this.pending) p.reject(err);
        this.pending.clear();
        reject(err);
      });

      ws.on('close', () => {
        const err = new Error('WebSocket closed');
        if (this._helloReject) {
          this._helloReject(err);
          this._helloResolve = null;
          this._helloReject = null;
        }
        for (const [, p] of this.pending) p.reject(err);
        this.pending.clear();
      });

      helloPromise
        .then(async (hello) => {
          if (hello.authentication && this.password) {
            await this.authenticate(hello.authentication.salt, hello.authentication.challenge);
          } else if (hello.authentication && !this.password) {
            console.warn(
              'Warning: server sent authentication challenge but STREAMERBOT_WS_PASSWORD is unset.',
            );
          }
          resolve(hello);
        })
        .catch(reject);
    });
  }

  /**
   * @param {string} salt
   * @param {string} challenge
   */
  async authenticate(salt, challenge) {
    const secret = createHash('sha256')
      .update(this.password + salt, 'utf8')
      .digest('base64');
    const authentication = createHash('sha256')
      .update(secret + challenge, 'utf8')
      .digest('base64');
    await this.request({ request: 'Authenticate', authentication });
  }

  /**
   * @param {Record<string, unknown>} body
   * @param {number} [timeoutMs]
   */
  request(body, timeoutMs = 15000) {
    return new Promise((resolve, reject) => {
      if (!this.ws || this.ws.readyState !== WebSocket.OPEN) {
        reject(new Error('WebSocket not open'));
        return;
      }
      const id = body.id ?? `fc-perf-${randomUUID()}`;
      const payload = { ...body, id };

      const timer = setTimeout(() => {
        this.pending.delete(id);
        reject(new Error(`Request timed out: ${body.request}`));
      }, timeoutMs);

      this.pending.set(id, {
        resolve: (v) => {
          clearTimeout(timer);
          resolve(v);
        },
        reject: (e) => {
          clearTimeout(timer);
          reject(e);
        },
      });

      this.ws.send(JSON.stringify(payload));
    });
  }

  /** @param {string} name */
  doActionByName(name) {
    return this.request({
      request: 'DoAction',
      action: { name },
      args: {},
    });
  }

  /**
   * @param {string} variable
   * @param {boolean} persisted
   */
  getGlobal(variable, persisted = true) {
    return this.request({
      request: 'GetGlobal',
      variable,
      persisted,
    });
  }

  /**
   * @param {boolean} persisted
   */
  getGlobals(persisted = true) {
    return this.request({
      request: 'GetGlobals',
      persisted,
    });
  }

  close() {
    this.ws?.close();
    this.ws = null;
  }
}

/**
 * Streamer.bot GetGlobal response shapes differ by version:
 * - Docs / some clients: `{ variable: { name, value, lastWrite } }`
 * - Streamer.bot 1.0.4: `{ variables: { [name]: { name, value, lastWrite } }, count }`
 *
 * @param {any} res
 * @param {string} name
 * @returns {{ name?: string, value?: unknown, lastWrite?: string } | null}
 */
function extractGlobalVariable(res, name) {
  if (!res || typeof res !== 'object') return null;
  if (res.variable && typeof res.variable === 'object') return res.variable;
  const map = res.variables;
  if (map && typeof map === 'object') {
    if (map[name] && typeof map[name] === 'object') return map[name];
    const keys = Object.keys(map);
    if (keys.length === 1 && map[keys[0]] && typeof map[keys[0]] === 'object') {
      return map[keys[0]];
    }
  }
  return null;
}

/**
 * @param {StreamerBotClient} client
 * @returns {Promise<{ fingerprint: string, raw: string | null, parsed: PerfLast | null, lastWrite: string | null, lastResponse: any }>}
 */
async function readPerfLast(client) {
  try {
    const res = await client.getGlobal(GLOBAL_NAME, true);
    const variable = extractGlobalVariable(res, GLOBAL_NAME);
    if (!variable) {
      return { fingerprint: '', raw: null, parsed: null, lastWrite: null, lastResponse: res };
    }
    const raw = variable.value == null ? null : String(variable.value);
    const lastWrite = variable.lastWrite != null ? String(variable.lastWrite) : null;
    const fingerprint = `${lastWrite ?? ''}|${raw ?? ''}`;
    let parsed = null;
    if (raw) {
      try {
        parsed = JSON.parse(raw);
      } catch {
        parsed = null;
      }
    }
    return { fingerprint, raw, parsed, lastWrite, lastResponse: res };
  } catch (err) {
    // GetGlobal errors when the variable does not exist yet.
    const msg = err instanceof Error ? err.message : String(err);
    if (/not exist|not found|missing|unknown|no variable/i.test(msg)) {
      return { fingerprint: '', raw: null, parsed: null, lastWrite: null, lastResponse: { error: msg } };
    }
    throw err;
  }
}

/**
 * @param {StreamerBotClient} client
 * @param {string} beforeFingerprint
 */
async function waitForPerfLast(client, beforeFingerprint) {
  const start = Date.now();
  /** @type {any} */
  let lastSnap = null;
  while (Date.now() - start < TIMEOUT_MS) {
    const snap = await readPerfLast(client);
    lastSnap = snap;
    if (snap.fingerprint && snap.fingerprint !== beforeFingerprint && snap.parsed) {
      return snap;
    }
    await sleep(POLL_MS);
  }

  const diag = [];
  diag.push(`Timed out after ${TIMEOUT_MS}ms waiting for ${GLOBAL_NAME} to update.`);
  if (lastSnap?.raw) {
    diag.push(
      `Global exists (lastWrite=${lastSnap.lastWrite ?? '?'}) but fingerprint did not change ` +
        `(same value as before DoAction, or JSON failed to parse).`,
    );
    diag.push(`Current value (truncated): ${String(lastSnap.raw).slice(0, 180)}`);
  } else if (lastSnap?.lastResponse) {
    diag.push(`Last GetGlobal response: ${JSON.stringify(lastSnap.lastResponse).slice(0, 400)}`);
  } else {
    diag.push(`${GLOBAL_NAME} was not present (or GetGlobal returned an empty/unrecognized shape).`);
  }
  diag.push(
    'Checks: (1) SB Log must show "[MENU] trigger …" right after DoAction — if missing, Method is disabled / wrong entry / Code failed to compile.',
  );
  diag.push(
    '(2) Method sub-action: enabled, CPHInline.Execute, Run on UI thread ON; Code sub-action DISABLED.',
  );
  diag.push(
    '(3) Redeploy with PerfTrace: .\\FluentConfig\\scripts\\Redeploy.ps1 -Configuration Release -PerfTrace',
  );
  diag.push(
    '(4) SB log should show [PerfTrace] + web-ready after open; raise PERF_TIMEOUT_MS if the UI is slow.',
  );
  throw new Error(diag.join('\n'));
}

/** @param {number} ms */
function sleep(ms) {
  return new Promise((r) => setTimeout(r, ms));
}

function promptEnter(message) {
  const rl = createInterface({ input: process.stdin, output: process.stdout });
  return new Promise((resolve) => {
    rl.question(message, () => {
      rl.close();
      resolve();
    });
  });
}

/**
 * @param {number} index
 * @param {PerfLast} perf
 */
function printRun(index, perf) {
  const label = index === 0 ? 'COLD' : 'WARM';
  const flag = perf.cold ? 'cold=true' : 'cold=false';
  console.log('');
  console.log(`── Run ${index + 1}/${RUNS} (${label}, ${flag}) ──`);
  console.log(`  totalMs: ${perf.totalMs}`);
  for (const m of perf.milestones ?? []) {
    const kind = m.kind === 'mark' ? 'mark' : 'phase';
    console.log(`  ${String(m.name).padEnd(28)} ${String(m.ms).padStart(6)}ms  (${kind})`);
  }
}

async function main() {
  console.log('FluentConfig perf benchmark');
  console.log(`  WS URL:     ${WS_URL}`);
  console.log(`  Action:     ${ACTION_NAME}`);
  console.log(`  Global:     ${GLOBAL_NAME} (persisted)`);
  console.log(`  Runs:       ${RUNS} (run 1 = cold after SB restart; later = warm)`);
  console.log('');
  console.log('Cold procedure: restart Streamer.bot, then start this script.');
  console.log('Warm procedure: close the FluentConfig window between runs (do not restart SB).');
  console.log('');

  const client = new StreamerBotClient(WS_URL, WS_PASSWORD);
  try {
    const hello = await client.connect();
    const ver = hello?.info?.version ?? '?';
    console.log(`Connected (Streamer.bot ${ver}).`);
  } catch (err) {
    console.error('Failed to connect:', err instanceof Error ? err.message : err);
    console.error(`Check Servers/Clients → WebSocket Server is running at ${WS_URL}`);
    process.exitCode = 1;
    return;
  }

  /** @type {PerfLast[]} */
  const results = [];

  try {
    for (let i = 0; i < RUNS; i++) {
      if (i > 0) {
        if (PAUSE_MS > 0) {
          console.log(`Waiting ${PAUSE_MS}ms before next run (close the menu window now)…`);
          await sleep(PAUSE_MS);
        } else if (process.stdin.isTTY) {
          await promptEnter(
            'Close the FluentConfig window, then press Enter for the next (warm) run… ',
          );
        } else {
          console.log('Non-TTY stdin; waiting 5s before next run (close the menu window)…');
          await sleep(5000);
        }
      }

      const before = await readPerfLast(client);
      if (before.raw) {
        console.log(
          `Baseline ${GLOBAL_NAME} present (lastWrite=${before.lastWrite ?? '?'}). Waiting for a new write…`,
        );
      } else {
        console.log(`Baseline: ${GLOBAL_NAME} not set yet.`);
      }

      console.log(`DoAction "${ACTION_NAME}"…`);
      let doActionRes;
      try {
        doActionRes = await client.doActionByName(ACTION_NAME);
      } catch (err) {
        const msg = err instanceof Error ? err.message : String(err);
        throw new Error(
          `DoAction failed for "${ACTION_NAME}": ${msg}\n` +
            'Create the action from FluentConfig/Host/PERF_BENCHMARK_ACTION.cs.txt ' +
            '(or PERF_BENCHMARK_COMPLETE_ACTION.cs.txt). Name must match; ' +
            'Execute C# Method → CPHInline.Execute with Run on UI thread.',
        );
      }
      if (doActionRes?.status === 'error') {
        throw new Error(
          `DoAction returned error for "${ACTION_NAME}": ${doActionRes.error ?? JSON.stringify(doActionRes)}`,
        );
      }
      // SB only confirms the action was found/queued. C# compile errors, a disabled
      // Method sub-action, or a thrown Execute() still look like success here.
      console.log(
        'DoAction accepted (queued only — does not prove C# ran). Polling GetGlobal…',
      );

      const after = await waitForPerfLast(client, before.fingerprint);
      results.push(after.parsed);
      printRun(i, after.parsed);
    }

    console.log('');
    console.log('══ Summary ══');
    for (let i = 0; i < results.length; i++) {
      const r = results[i];
      const label = i === 0 ? 'cold' : 'warm';
      console.log(
        `  ${label.padEnd(4)}  totalMs=${String(r.totalMs).padStart(5)}  reportedCold=${r.cold}`,
      );
    }
  } catch (err) {
    console.error(err instanceof Error ? err.message : err);
    process.exitCode = 1;
  } finally {
    client.close();
  }
}

main();
