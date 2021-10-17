using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;
using VocabularyReminderAPI.Authentication;
using VocabularyReminderAPI.Models;

namespace VocabularyReminderAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class DictionaryController : ControllerBase
    {
        private readonly VocaContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DictionaryController(VocaContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);

            var result = await _context.Set<Vocabulary>().AsNoTracking()
                            .Where(e => e.UserId == user.Id)
                            .ToListAsync();
            return Ok(result);
        }
    }
}
