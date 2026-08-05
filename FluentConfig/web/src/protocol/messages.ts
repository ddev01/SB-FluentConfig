/**
 * Wire envelope + push event payloads.
 * Mirror of `FluentConfig/Host/Protocol/WireMessage.cs` and `PushEvents.cs`.
 */

import type { SchemaNode, UiDocument } from './schema';

export const WireKinds = {
  Request: 'request',
  Response: 'response',
  Event: 'event',
} as const;

export type WireKind = (typeof WireKinds)[keyof typeof WireKinds];

export interface RpcError {
  code: string;
  message: string;
}

/** RPC request (either direction). */
export interface RpcRequest {
  kind: 'request';
  id: number;
  method: string;
  params?: unknown;
}

/** RPC response; carries either result or error. */
export interface RpcResponse {
  kind: 'response';
  id: number;
  result?: unknown;
  error?: RpcError;
}

/** One-way push event. */
export interface PushEventMessage {
  kind: 'event';
  event: string;
  payload?: unknown;
}

export type WireMessage = RpcRequest | RpcResponse | PushEventMessage;

export const PushEventNames = {
  Bootstrap: 'bootstrap',
  Progress: 'progress',
  ValuesPatch: 'values.patch',
  UpdateAvailable: 'update.available',
  SchemaPatch: 'schema.patch',
} as const;

export type PushEventName = (typeof PushEventNames)[keyof typeof PushEventNames];

export interface ProgressPayload {
  id: string;
  title?: string;
  message?: string;
  /** 0–100, or use current/total. */
  percent?: number;
  current?: number;
  total?: number;
  done?: boolean;
}

export interface ValuesPatchPayload {
  /** Sparse path→value map (e.g. { "rate_value": 1.5 }). */
  paths?: Record<string, unknown>;
  /** Deep-merge partial values object (alternative to paths). */
  values?: Record<string, unknown>;
}

export interface UpdateAvailablePayload {
  noticeId?: string;
  currentVersion: string;
  latestVersion: string;
  releaseNotes?: string;
  downloadUrl?: string;
  repo?: string;
  /** Default `'self'` for back-compat. */
  mode?: 'self' | 'notify';
  /** Release HTML page; used when `mode === 'notify'`. */
  releasePageUrl?: string;
}

export interface SchemaPatchPayload {
  sectionId?: string;
  nodeId?: string;
  node: SchemaNode;
}

/** Typed bootstrap event. */
export interface BootstrapEvent {
  kind: 'event';
  event: 'bootstrap';
  payload: UiDocument;
}
