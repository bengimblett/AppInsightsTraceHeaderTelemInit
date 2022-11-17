namespace Demo
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNetCore.Http;
    using Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers;

    /// <summary>
    /// This telemetry initializer extracts upstream proxy log references from provided request headers and populates telemetry custom properties with the value(s).
    /// Assuming Log Analytics is used for upstream proxy logs, this allows KQL / workbook based correlation of Application Insights telemetry to upstream proxy logs.
    /// If another logging destination is used for the upstream proxy, then the log data is still correlated against the Application Insights telemetry
    /// </summary>
    public class UpstreamProxyTraceHeaderTelemetryInitializer : TelemetryInitializerBase
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="UpstreamProxyTraceHeaderTelemetryInitializer" /> class.
        /// </summary>
        /// <param name="httpContextAccessor">Accessor to provide HttpContext corresponding to telemetry items.</param>
        /// <param name="headerNames">Set header names, containing correlation values, to search. Defaults to Azure App Gateway and Front Door service log reference headers </param>
        public UpstreamProxyTraceHeaderTelemetryInitializer(IHttpContextAccessor httpContextAccessor, ICollection<string>? headerNames = default(ICollection<string>))
             : base(httpContextAccessor)
        {
            if ( headerNames == null){
                headerNames =new string[] { "x-azure-ref", "x-appgw-trace-id"};
            }
            HeaderNames = headerNames;
        }


        /// <summary>
        /// Gets comma separated list of request header names that contain upstream log references.
        /// </summary>
        public ICollection<string> HeaderNames { get; }


        /// <inheritdoc />
        protected override void OnInitializeTelemetry(HttpContext platformContext, RequestTelemetry requestTelemetry, ITelemetry telemetry)
        {
            if ( null == this.HeaderNames || this.HeaderNames.Count==0) {
                    return;
            }
            if (telemetry == null)  {
                throw new ArgumentNullException(nameof(telemetry));
            }
            if (requestTelemetry == null) {
                throw new ArgumentNullException(nameof(requestTelemetry));
            }
            if ( telemetry== requestTelemetry ) { // only do this for the request telemetry 
                if (platformContext == null) {
                    throw new ArgumentNullException(nameof(platformContext));
                }                 
                if (platformContext.Request?.Headers != null && platformContext.Request?.Headers.Count> 0) {
                    foreach (var name in this.HeaderNames) {
                        var value = GetHeaderValue(name,platformContext.Request.Headers);

                        if ( !String.IsNullOrEmpty(value)) {
                            AddReference(requestTelemetry,name, value);
                        }
                    }
                }
            }
        }

        private void AddReference(RequestTelemetry requestTelemetry, string headerName, string headerValue){
            if ( null == requestTelemetry ){
                return;
            }
            requestTelemetry.Properties.Add(headerName, headerValue);
        }

        private string GetHeaderValue(string headerNameToSearch, IHeaderDictionary requestHeaders){

            if ( null == requestHeaders || requestHeaders.Count== 0){
                 return string.Empty;
            }
            if (requestHeaders.ContainsKey(headerNameToSearch)) {
                return requestHeaders[headerNameToSearch];
            }
            return string.Empty;
        }

    }
}