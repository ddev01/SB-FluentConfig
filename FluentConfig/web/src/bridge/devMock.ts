/**
 * DEV-only mock bridge entry. Production builds alias this file to
 * `devMock.empty.ts` so mockBridge/mockDocument never enter the Release graph.
 */
export { getMockWebView as tryGetMockWebView } from './mockBridge';
