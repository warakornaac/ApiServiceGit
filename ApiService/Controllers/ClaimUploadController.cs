using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace ApiService.Controllers
{
    public class ClaimUploadController : ApiController
    {
        private readonly ApiServerController _apiServerService;

        public ClaimUploadController()
        {
            _apiServerService = new ApiServerController();
        }


    }
}