using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scrabble.Core;
using Scrabble.Core.AI;
using Scrabble.Core.Types;
using Scrabble.Server.Data;
using Scrabble.Server.Utility;
using Scrabble.Shared;

namespace Scrabble.Server.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TwoLetterWordsController : Controller
    {

        public TwoLetterWordsController()
        {
        }



        [HttpGet()]
        public ActionResult<List<string>> Get()
        {
            return Ok(WordLookupSingleton.instance.TwoLetterWords);
        }

    }
}
