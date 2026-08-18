using System;
using System.Threading.Tasks;
using System.Web.Http;
using ApiService.Services;
using ApiService.Jobs;

namespace ApiService.Controllers
{
    public class JobController : ApiController
    {
        [HttpPost]
        [Route("job/sync-stock")]
        public async Task<IHttpActionResult> SyncStock(string key) {
            // Simple shared-secret check so this endpoint cannot be triggered
            // by anyone who happens to guess the URL. Store the real secret
            // in Web.config, not in source control.
            var expectedKey = System.Configuration.ConfigurationManager.AppSettings["JobTriggerKey"];
            if (string.IsNullOrEmpty(expectedKey) || key != expectedKey) {
                return Unauthorized();
            }

            try {
                var job = new StockSyncJob();
                await job.RunAsync();

                return Ok(new {
                    success = true,
                    message = "Stock sync completed",
                    cacheCount = Services.StockCacheService.Instance.Count,
                    lastUpdated = Services.StockCacheService.Instance.LastUpdated
                });
            }
            catch (Exception ex) {
                // Log the full exception with your existing logging framework here.
                return InternalServerError(ex);
            }
        }
    }
}