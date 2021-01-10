import HttpStatus = require("http-status-codes");

export function getHttpStatusCode(error) {
    const resCode: number = error.code ?? HttpStatus.INTERNAL_SERVER_ERROR;
    return (error.toString().includes("404") ? HttpStatus.NOT_FOUND : resCode);
}
