export interface ErrorResponse {
    error: Error;
}
interface Error {
    code: string;
    message: string;
    target?: string;
    details?: Error[];
    innererror?: InnerError;
}
interface InnerError {
    code?: string;
    innerError?: InnerError;
}
