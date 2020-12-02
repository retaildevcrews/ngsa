/**
 * Handles /robots*.txt requests
 * App Service sends a warmup request of /robots8327.txt (where 8327 is a random number)
 * this causes a 404 error which appears in Azure Monitor as a false issue
 * this also handles a real /robots.txt request to prevent indexing
 */
export function robotsHandler(req: any, res: any, next) {
    const robotsResponse = "# Prevent indexing\r\nUser-agent: *\r\nDisallow: /\r\n";
    const path: string = req.url.substring(1);

    if (!path.includes("/") && path.toLocaleLowerCase().startsWith("robots") && path.toLocaleLowerCase().endsWith(".txt") ) {
        res.writeHead(200, {
            "Content-Length": Buffer.byteLength(robotsResponse),
            "Content-Type": "text/plain",
        });
        res.write(robotsResponse);

        res.end();
    } else {
        next();
    }
}
