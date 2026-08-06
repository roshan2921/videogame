using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VideoGameApi.Data;

namespace VideoGameApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VideoGameController(VideoGameDbContext context) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<List<VideoGame>>> GetVideoGames()
        {
            return Ok(await context.VideoGames.ToListAsync());
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<VideoGame>> GetVideoGameById(int id)
        {
            var videoGame = await context.VideoGames.FindAsync(id);
            if (videoGame is null)
                return NotFound();

            return Ok(videoGame);
        }

        [HttpPost]
        public async Task<ActionResult<VideoGame>> CreateVideoGame(VideoGame newGame)
        {
            if (newGame is null)
                return BadRequest();

            context.VideoGames.Add(newGame);
            await context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetVideoGameById), new { id = newGame.Id }, newGame);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateVideoGame(int id, VideoGame updatedGame)
        {
            var videoGame = await context.VideoGames.FindAsync(id);
            if (videoGame is null)
                return NotFound();

            videoGame.Title = updatedGame.Title;
            videoGame.Platform = updatedGame.Platform;
            videoGame.Developer = updatedGame.Developer;
            videoGame.Publisher = updatedGame.Publisher;

            await context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVideoGame(int id)
        {
            var videoGame = await context.VideoGames.FindAsync(id);
            if (videoGame is null)
                return NotFound();

            context.VideoGames.Remove(videoGame);
            await context.SaveChangesAsync();

            return NoContent();
        }
    }
}
