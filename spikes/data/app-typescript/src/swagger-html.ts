// HTML for Swagger UI

export const html = `<!DOCTYPE html>
                <html lang="en">
                <head>
                    <meta charset="UTF-8">
                    <title>Swagger UI</title>
                    <link rel="stylesheet" type="text/css" href="/node_modules/swagger-ui-dist/swagger-ui.css" >
                    <link rel="icon" type="image/png"
                    href="/node_modules/swagger-ui-dist/favicon-32x32.png" sizes="32x32" />
                    <link rel="icon" type="image/png"
                    href="/node_modules/swagger-ui-dist/favicon-16x16.png" sizes="16x16" />
                    <style>
                    html
                    {
                        box-sizing: border-box;
                        overflow: -moz-scrollbars-vertical;
                        overflow-y: scroll;
                    }

                    *,
                    *:before,
                    *:after
                    {
                        box-sizing: inherit;
                    }

                    body
                    {
                        margin:0;
                        background: #fafafa;
                    }
                    </style>
                </head>

                <body>
                    <div id="swagger-ui"></div>

                    <script src="/node_modules/swagger-ui-dist/swagger-ui-bundle.js"> </script>
                    <script src="/node_modules/swagger-ui-dist/swagger-ui-standalone-preset.js"> </script>
                    <script>
                    window.onload = function() {
                        // begin Swagger UI call region
                        const ui = SwaggerUIBundle({
                            url: '/swagger/ngsa.json',
                            dom_id: '#swagger-ui',
                            deepLinking: true,
                            presets: [
                            SwaggerUIBundle.presets.apis,
                            SwaggerUIStandalonePreset
                            ],
                            plugins: [
                            SwaggerUIBundle.plugins.DownloadUrl
                            ],
                            layout: "StandaloneLayout"
                        })
                        // end Swagger UI call region

                        window.ui = ui
                    }
                </script>
                </body>
                </html>`;
