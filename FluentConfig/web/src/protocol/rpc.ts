/**
 * RPC method names and param/result shapes.
 * Mirror of `FluentConfig/Host/Protocol/RpcMethods.cs`.
 */

import type { DropdownOption, PillItemSchema, SettingsValues } from './schema';

export const RpcMethods = {
  Save: 'save',
  DropdownRefresh: 'dropdown.refresh',
  ButtonClick: 'button.click',
  FilepathBrowse: 'filepath.browse',
  PillChanged: 'pill.changed',
  UpdateStage: 'update.stage',
  UpdateDismiss: 'update.dismiss',
  Log: 'log',
  DialogConfirm: 'dialog.confirm',
  DialogPopup: 'dialog.popup',
  Toast: 'toast.show',
  /** Host→Web: native close asks whether to allow. Result: WindowCloseRequestedResult. */
  WindowCloseRequested: 'window.closeRequested',
  /** Web→Host: in-app Exit (or confirmed close). Params: WindowCloseParams. */
  WindowClose: 'window.close',
  /** Web→Host fire-and-forget: open external URL in the OS browser. */
  ShellOpenUrl: 'shell.openUrl',
  /** Web→Host fire-and-forget: perf phase mark (e.g. web-ready). */
  PerfMark: 'perf.mark',
} as const;

export type RpcMethod = (typeof RpcMethods)[keyof typeof RpcMethods];

export interface SaveParams {
  values: SettingsValues;
}

export interface DropdownRefreshParams {
  saveKey: string;
}

export interface DropdownRefreshResult {
  options: DropdownOption[];
}

export interface ButtonClickParams {
  buttonId: string;
  values: SettingsValues;
}

export interface FilepathBrowseParams {
  saveKey: string;
}

export interface FilepathBrowseResult {
  path?: string | null;
}

export interface PillChangedParams {
  saveKey: string;
  action: 'add' | 'remove' | 'rename';
  name: string;
  previousName?: string;
  items: string[];
}

export interface PillChangedResult {
  items?: PillItemSchema[];
  removedKeys?: string[];
}

export interface UpdateStageParams {
  downloadUrl: string;
  noticeId?: string;
}

export interface UpdateDismissParams {
  noticeId?: string;
}

export interface LogParams {
  message: string;
}

export interface ConfirmParams {
  title: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
}

export interface ConfirmResult {
  confirmed: boolean;
}

export interface PopupParams {
  title: string;
  message: string;
}

export interface ToastParams {
  message: string;
}

/** Host→Web: native title-bar close asks whether the web side allows it. */
export interface WindowCloseRequestedResult {
  allowClose: boolean;
  dontRemindAgain: boolean;
}

/** Web→Host: in-app Exit (or already-confirmed close). */
export interface WindowCloseParams {
  alreadyConfirmed: boolean;
  /** Persist per-title discard reminder skip when user checked the box on Exit. */
  dontRemindAgain?: boolean;
}

export interface ShellOpenUrlParams {
  url: string;
}

export interface PerfMarkParams {
  name: string;
}
