/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** Base URL of the BCAS Web API, e.g. https://localhost:7216 */
  readonly VITE_API_BASE_URL?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
