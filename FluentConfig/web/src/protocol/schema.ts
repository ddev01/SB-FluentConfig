/**
 * TypeScript mirror of `FluentConfig/Host/Protocol/`.
 *
 * These types are the web-side half of the shared host↔UI message contract.
 * Keep them structurally identical to the C# DTOs' JSON form (camelCase fields,
 * same optionality). Sync is manual for now — codegen from a single schema is a
 * reasonable future improvement, not in scope for Phase 0.
 *
 * See also: `FluentConfig/PROTOCOL.md`
 */

export type ColorScheme = 'light' | 'dark' | 'system';

/** Conditional display (ShowWhen / WithVisibility). */
export interface VisibilityCondition {
  saveKey: string;
  /** Expected value; typically `true` for toggle-gated visibility. Absent when using `operator`. */
  equals?: boolean | string | number | null;
  inverted?: boolean;
  /** Comparator: `gte` | `lte` | `gt` | `lt`. Absent = legacy equals. */
  operator?: 'gte' | 'lte' | 'gt' | 'lt';
  /** Literal numeric comparand when `operator` is set. Mutually exclusive with `compareKey`. */
  value?: number;
  /** Live saveKey whose value is the right-hand comparand. Mutually exclusive with `value`. */
  compareKey?: string;
}

/** Per-node layout hint (Span / Size). Width fields are host-resolved CSS values. */
export interface LayoutHint {
  /** CSS grid-column span. */
  span?: number;
  /** Resolved CSS `width` (e.g. `fit-content`, `50%`, `80px`). */
  width?: string;
  /** Resolved CSS `min-width`. */
  minWidth?: string;
  /** Resolved CSS `max-width`. */
  maxWidth?: string;
  /** Flex grow (Row only). `true` → 1, `false` → 0. */
  grow?: boolean;
  /** Flex shrink (Row only). `true` → 1, `false` → 0. */
  shrink?: boolean;
}

/** Layout container spec on a group node (Grid / Row). */
export interface GridSpec {
  /** `grid` (CSS grid) or `row` (flex wrap). Default `grid`. */
  mode?: 'grid' | 'row';
  /** Equal columns when mode is `grid`. */
  columns?: number;
  /** Gap on Tailwind spacing scale (gap * 4px). Default 3. */
  gap?: number;
  /** CSS `align-items`: start | center | end | stretch | baseline. */
  align?: 'start' | 'center' | 'end' | 'stretch' | 'baseline';
}

export type SchemaNodeType =
  | 'toggle'
  | 'textbox'
  | 'slider'
  | 'number-input'
  | 'dropdown'
  | 'color-picker'
  | 'duration-input'
  | 'filepath'
  | 'pill-input'
  | 'dynamic-textboxes'
  | 'repeatable-rows'
  | 'button'
  | 'description'
  | 'title'
  | 'separator'
  | 'update-notice'
  | 'connection-status'
  | 'group';

export interface SchemaNodeBase {
  type: SchemaNodeType;
  visibility?: VisibilityCondition;
  layout?: LayoutHint;
}

export interface ExclusiveToggleOptions {
  options: string[];
  /** Max toggles that can be true. Min 1. Default 1. */
  maxSelected?: number;
  defaultIndex?: number;
  defaultIndices?: number[];
}

export interface ToggleNode extends SchemaNodeBase {
  type: 'toggle';
  id?: string;
  label: string;
  saveKey: string;
  hint?: string;
  /** Plain on/off default. Ignored when `exclusive` is set. */
  defaultValue?: boolean;
  exclusive?: ExclusiveToggleOptions;
}

export interface TextboxNode extends SchemaNodeBase {
  type: 'textbox';
  id?: string;
  label: string;
  saveKey: string;
  hint?: string;
  defaultValue?: string;
  password?: boolean;
  multiline?: boolean;
}

export interface SliderNode extends SchemaNodeBase {
  type: 'slider';
  id?: string;
  label: string;
  saveKey: string;
  hint?: string;
  min: number;
  max: number;
  defaultValue?: number;
}

/** Covers Input / NumberInput / IntegerInput. */
export interface NumberInputNode extends SchemaNodeBase {
  type: 'number-input';
  id?: string;
  label: string;
  saveKey: string;
  hint?: string;
  valueType?: 'string' | 'int' | 'double' | 'float';
  min?: number;
  max?: number;
  step?: number;
  defaultValue?: string | number;
  stepper?: boolean;
}

export interface DropdownOption {
  value: string;
  display: string;
}

export interface DropdownNode extends SchemaNodeBase {
  type: 'dropdown';
  id?: string;
  label: string;
  saveKey: string;
  hint?: string;
  options?: DropdownOption[];
  /** When set, display is stored in saveKey and underlying value in valueSaveKey. */
  valueSaveKey?: string;
  refreshable?: boolean;
  defaultIndex?: number;
  defaultByValue?: string;
  /** Explicit default string (.Default(string) / AllowCustom). */
  defaultValue?: string;
  searchable?: boolean;
  allowCustom?: boolean;
  multiple?: boolean;
  defaultValues?: string[];
}

export interface ColorPickerNode extends SchemaNodeBase {
  type: 'color-picker';
  id?: string;
  label: string;
  saveKey: string;
  hint?: string;
  defaultValue?: string;
}

export interface DurationInputNode extends SchemaNodeBase {
  type: 'duration-input';
  id?: string;
  label: string;
  saveKey: string;
  hint?: string;
  /** e.g. "30seconds", "5minutes", "permanent" */
  defaultValue?: string;
  permanentOption?: boolean;
}

export interface FilepathNode extends SchemaNodeBase {
  type: 'filepath';
  id?: string;
  label: string;
  saveKey: string;
  hint?: string;
  defaultValue?: string;
  /** When true, omit the built-in Browse button. */
  hideBrowse?: boolean;
  /** When false, skip existence check. Default true. Empty path is always allowed. */
  mustExist?: boolean;
  /** Allowed extensions including the dot (e.g. ".gif"). */
  accept?: string[];
}

export interface DynamicTextboxesNode extends SchemaNodeBase {
  type: 'dynamic-textboxes';
  id?: string;
  label: string;
  saveKey: string;
  hint?: string;
  preset?: string[];
  allowDuplicates?: boolean;
}

export interface ButtonNode extends SchemaNodeBase {
  type: 'button';
  /** Stable id for button.click RPC (buttons have no saveKey). */
  id: string;
  label?: string;
  hint?: string;
  text?: string;
  color?: string;
}

export interface DescriptionNode extends SchemaNodeBase {
  type: 'description';
  id?: string;
  text: string;
}

export interface TitleNode extends SchemaNodeBase {
  type: 'title';
  id?: string;
  text: string;
}

export interface SeparatorNode extends SchemaNodeBase {
  type: 'separator';
  id?: string;
}

/**
 * Extension update modal. Host fills after CheckForUpdate;
 * UI never talks to GitHub — opens guide/release URLs via `shell.openUrl`.
 */
export interface UpdateNoticeNode extends SchemaNodeBase {
  type: 'update-notice';
  id?: string;
  currentVersion: string;
  latestVersion: string;
  releaseNotes?: string;
  /** Unused for extension notices; kept for protocol compatibility. */
  downloadUrl?: string;
  /** owner/name form (informational). */
  repo?: string;
  dismissible?: boolean;
  /** Default `'notify'` for in-menu extension notices. */
  mode?: 'self' | 'notify';
  /** Release HTML page (fallback for How to update). */
  releasePageUrl?: string;
  /** Preferred “How to update” URL (guide or release page). */
  updateGuideUrl?: string;
}

export type ConnectionStatusValue =
  | 'connected'
  | 'disconnected'
  | 'connecting'
  | 'error';

/** Live connection indicator; status updates arrive via `schema.patch`. */
export interface ConnectionStatusNode extends SchemaNodeBase {
  type: 'connection-status';
  id: string;
  label: string;
  hint?: string;
  status: ConnectionStatusValue;
  statusText?: string;
  buttonText?: string;
  buttonId?: string;
}

/**
 * Nested schema group: visibility gate, layout container (Grid/Row), and/or RepeatFor index gate.
 * Pure data — no UI-framework container references.
 */
export interface GroupNode extends SchemaNodeBase {
  type: 'group';
  id?: string;
  children: SchemaNode[];
  /** When set, children render in a CSS grid/flex container instead of the indented stack. */
  grid?: GridSpec;
  /**
   * Left-rail chrome. Omitted/`true` = indented rail (toggle gates).
   * `false` = flat (value-equals, comparator, RepeatFor).
   */
  indented?: boolean;
}

export interface PillItemSchema {
  name: string;
  children: SchemaNode[];
}

/**
 * Pill list with per-item nested sub-panels as schema nodes (never Panel/WPF objects).
 * Relative saveKeys in itemTemplate may use "{name}" expanded to the pill item name.
 */
export interface PillInputNode extends SchemaNodeBase {
  type: 'pill-input';
  id?: string;
  label: string;
  saveKey: string;
  hint?: string;
  itemTemplate?: SchemaNode[];
  /** Host-expanded schemas for existing pills; preferred over client-side template expansion when present. */
  items?: PillItemSchema[];
}

/** Repeatable rows: each row is an object under saveKey[i]; rowSchema uses relative keys. */
export interface RepeatableRowsNode extends SchemaNodeBase {
  type: 'repeatable-rows';
  id?: string;
  saveKey: string;
  rowSchema: SchemaNode[];
}

export type SchemaNode =
  | ToggleNode
  | TextboxNode
  | SliderNode
  | NumberInputNode
  | DropdownNode
  | ColorPickerNode
  | DurationInputNode
  | FilepathNode
  | PillInputNode
  | DynamicTextboxesNode
  | RepeatableRowsNode
  | ButtonNode
  | DescriptionNode
  | TitleNode
  | SeparatorNode
  | UpdateNoticeNode
  | ConnectionStatusNode
  | GroupNode;

export interface SectionSchema {
  id: string;
  title: string;
  children: SchemaNode[];
}

/** Settings values blob — nested JSON object; paths match saveKey conventions. */
export type SettingsValues = Record<string, unknown>;

export interface UiDocument {
  title: string;
  version: string;
  colorScheme?: ColorScheme;
  sections: SectionSchema[];
  values: SettingsValues;
  /** FluentConfig framework version (host-injected, not author-configurable). */
  frameworkVersion?: string;
  /** FluentConfig repo URL (host-injected). */
  repoUrl?: string;
  /** Skip discard-changes dialog on close (from CPH prefs). */
  dontRemindDiscard?: boolean;
}
