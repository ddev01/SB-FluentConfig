import type {
  ProgressPayload,
  SchemaNode,
  SectionSchema,
  SettingsValues,
  UiDocument,
  UpdateAvailablePayload,
  ValuesPatchPayload,
} from '../protocol';
import { PushEventNames, RpcMethods } from '../protocol';
import { cloneJson } from '../lib/clone';
import { applyPathMap, deepMerge, getPath, setPath } from '../lib/paths';
import { applyColorScheme, watchSystemScheme } from '../lib/theme';
import type { RpcClient } from '../rpc/client';

export type ToastItem = { id: number; message: string };

/** Shared app state (Svelte 5 runes module). */
class AppStore {
  document = $state<UiDocument | null>(null);
  values = $state<SettingsValues>({});
  progress = $state<ProgressPayload | null>(null);
  activeSectionId = $state<string | null>(null);
  ready = $state(false);
  usingMock = $state(false);
  toasts = $state<ToastItem[]>([]);
  saving = $state(false);
  saveMessage = $state<string | null>(null);

  private rpc: RpcClient | null = null;
  private unsubSystem: (() => void) | null = null;
  private toastSeq = 0;
  private disposed = false;

  bind(rpc: RpcClient, usingMock: boolean): void {
    this.rpc = rpc;
    this.usingMock = usingMock;

    rpc.on(PushEventNames.Bootstrap, (payload) => {
      this.applyBootstrap(payload as UiDocument);
    });
    rpc.on(PushEventNames.Progress, (payload) => {
      const p = payload as ProgressPayload;
      this.progress = p;
      if (p.done) {
        window.setTimeout(() => {
          if (this.progress?.id === p.id && this.progress.done) {
            this.progress = null;
          }
        }, 600);
      }
    });
    rpc.on(PushEventNames.ValuesPatch, (payload) => {
      this.applyValuesPatch(payload as ValuesPatchPayload);
    });
    rpc.on(PushEventNames.UpdateAvailable, (payload) => {
      this.applyUpdateAvailable(payload as UpdateAvailablePayload);
    });
    rpc.on(PushEventNames.SchemaPatch, (payload) => {
      const patch = payload as {
        sectionId?: string;
        nodeId?: string;
        node: SchemaNode;
      };
      this.applySchemaPatch(patch);
    });

    rpc.setIncomingRequestHandler(async (method, params) => {
      if (method === RpcMethods.DialogConfirm) {
        const p = params as {
          title: string;
          message: string;
          confirmText?: string;
          cancelText?: string;
        };
        const confirmed = window.confirm(`${p.title}\n\n${p.message}`);
        return { confirmed };
      }
      if (method === RpcMethods.DialogPopup) {
        const p = params as { title: string; message: string };
        window.alert(`${p.title}\n\n${p.message}`);
        return { ok: true };
      }
      if (method === RpcMethods.Toast) {
        const p = params as { message: string };
        this.pushToast(p.message);
        return { ok: true };
      }
      throw new Error(`Unhandled host request: ${method}`);
    });
  }

  dispose(): void {
    this.disposed = true;
    this.unsubSystem?.();
    this.unsubSystem = null;
    this.rpc = null;
  }

  getValue(saveKey: string): unknown {
    return getPath(this.values, saveKey);
  }

  setValue(saveKey: string, value: unknown): void {
    const next = cloneJson(this.values) as SettingsValues;
    setPath(next, saveKey, value);
    this.values = next;
  }

  /** Replace options on a dropdown node in the live document. */
  patchDropdownOptions(saveKey: string, options: { value: string; display: string }[]): void {
    if (!this.document) return;
    const doc = cloneJson(this.document);
    const visit = (nodes: SchemaNode[]): boolean => {
      for (const node of nodes) {
        if (node.type === 'dropdown' && node.saveKey === saveKey) {
          node.options = options;
          return true;
        }
        if (node.type === 'group' && visit(node.children)) return true;
        if (node.type === 'pill-input') {
          if (node.items) {
            for (const item of node.items) {
              if (visit(item.children)) return true;
            }
          }
          if (node.itemTemplate && visit(node.itemTemplate)) return true;
        }
        if (node.type === 'repeatable-rows' && visit(node.rowSchema)) return true;
      }
      return false;
    };
    for (const section of doc.sections) {
      if (visit(section.children)) {
        this.document = doc;
        return;
      }
    }
  }

  patchPillItems(
    saveKey: string,
    items: { name: string; children: SchemaNode[] }[],
  ): void {
    if (!this.document) return;
    const doc = cloneJson(this.document);
    for (const section of doc.sections) {
      for (const node of section.children) {
        if (node.type === 'pill-input' && node.saveKey === saveKey) {
          node.items = items;
          this.document = doc;
          return;
        }
      }
    }
  }

  removeUpdateNotice(noticeId?: string): void {
    if (!this.document) return;
    const doc = cloneJson(this.document);
    for (const section of doc.sections) {
      section.children = section.children.filter((n) => {
        if (n.type !== 'update-notice') return true;
        if (!noticeId) return false;
        return n.id !== noticeId;
      });
    }
    this.document = doc;
  }

  async save(): Promise<void> {
    if (!this.rpc) return;
    this.saving = true;
    this.saveMessage = null;
    try {
      await this.rpc.request(RpcMethods.Save, { values: this.values });
      this.saveMessage = 'Saved';
      window.setTimeout(() => {
        if (this.saveMessage === 'Saved') this.saveMessage = null;
      }, 2000);
    } catch (err) {
      this.saveMessage = err instanceof Error ? err.message : 'Save failed';
    } finally {
      this.saving = false;
    }
  }

  client(): RpcClient {
    if (!this.rpc) throw new Error('RpcClient not bound');
    return this.rpc;
  }

  pushToast(message: string): void {
    const id = ++this.toastSeq;
    this.toasts = [...this.toasts, { id, message }];
    window.setTimeout(() => {
      this.toasts = this.toasts.filter((t) => t.id !== id);
    }, 3200);
  }

  private applyBootstrap(doc: UiDocument): void {
    if (this.disposed) return;
    this.document = doc;
    this.values = cloneJson(doc.values) as SettingsValues;
    this.activeSectionId = doc.sections[0]?.id ?? null;
    this.ready = true;
    this.unsubSystem?.();
    applyColorScheme(doc.colorScheme);
    this.unsubSystem = watchSystemScheme(doc.colorScheme);
  }

  private applyValuesPatch(patch: ValuesPatchPayload): void {
    const next = cloneJson(this.values) as SettingsValues;
    if (patch.paths) applyPathMap(next, patch.paths);
    if (patch.values) deepMerge(next, patch.values as SettingsValues);
    this.values = next;
  }

  private applyUpdateAvailable(payload: UpdateAvailablePayload): void {
    if (!this.document) return;
    const doc = cloneJson(this.document);
    const notice: SchemaNode = {
      type: 'update-notice',
      id: payload.noticeId ?? 'update-notice',
      currentVersion: payload.currentVersion,
      latestVersion: payload.latestVersion,
      releaseNotes: payload.releaseNotes,
      downloadUrl: payload.downloadUrl,
      repo: payload.repo,
      dismissible: true,
    };
    const first = doc.sections[0];
    if (first) {
      first.children = [
        notice,
        ...first.children.filter((n) => n.type !== 'update-notice'),
      ];
    }
    this.document = doc;
  }

  private applySchemaPatch(patch: {
    sectionId?: string;
    nodeId?: string;
    node: SchemaNode;
  }): void {
    if (!this.document) return;
    const doc = cloneJson(this.document);
    const replaceIn = (nodes: SchemaNode[]): boolean => {
      for (let i = 0; i < nodes.length; i++) {
        const n = nodes[i]!;
        const id = 'id' in n ? n.id : undefined;
        if (patch.nodeId && id === patch.nodeId) {
          nodes[i] = patch.node;
          return true;
        }
        if (n.type === 'group' && replaceIn(n.children)) return true;
        if (n.type === 'pill-input' && n.items) {
          for (const item of n.items) {
            if (replaceIn(item.children)) return true;
          }
        }
      }
      return false;
    };

    const sections: SectionSchema[] = patch.sectionId
      ? doc.sections.filter((s) => s.id === patch.sectionId)
      : doc.sections;

    for (const section of sections) {
      if (replaceIn(section.children)) {
        this.document = doc;
        return;
      }
    }
    // Append to named/first section if no nodeId match
    if (!patch.nodeId) {
      const target =
        doc.sections.find((s) => s.id === patch.sectionId) ?? doc.sections[0];
      if (target) {
        target.children.push(patch.node);
        this.document = doc;
      }
    }
  }
}

export const appStore = new AppStore();
