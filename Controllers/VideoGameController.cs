using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace VideoGameApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VideoGameController : ControllerBase
    {
        static private List<VideoGame> videoGames = new List<VideoGame>
        {
            new VideoGame
            {
                Id = 1,
                Title = "Spider-Man 2",
                Platform = "PS5",
                Developer = "Insomniac Games",
                Publisher = "Sony Interactive Entertainment"
            },
            new VideoGame
            {
                Id = 2,
                Title = "The Legend of Zelda: Breath of the Wild",
                Platform = "Nintendo Switch",
                Developer = "Nintendo EPD",
                Publisher = "Nintendo"
            },
            new VideoGame
            {
                Id = 3,
                Title = "Elden Ring",
                Platform = "PC",
                Developer = "FromSoftware",
                Publisher = "Bandai Namco Entertainment"
            }
        };

        [HttpGet]
        public ActionResult<List<VideoGame>> GetVideoGames()
        {
            return Ok(videoGames);
        }

        [HttpGet("{id}")]
        public ActionResult<VideoGame> GetVideoGamesById(int id)
        {
            var videoGame = videoGames.FirstOrDefault(g => g.Id == id);
            if (videoGame is null)
                return NotFound();

            return Ok(videoGame);
        }

        [HttpPost]
        public ActionResult<VideoGame> CreateVideoGame(VideoGame newGame)
        {
            if (newGame is null)
                return BadRequest();

            newGame.Id = videoGames.Max(g => g.Id) + 1;
            videoGames.Add(newGame);

            return CreatedAtAction(nameof(GetVideoGamesById), new { id = newGame.Id }, newGame);
        }

        [HttpPut("{id}")]
        public IActionResult UpdateVideoGame(int id, VideoGame updatedGame)
        {
            var videoGame = videoGames.FirstOrDefault(g => g.Id == id);
            if (videoGame is null)
                return NotFound();

            videoGame.Title = updatedGame.Title;
            videoGame.Platform = updatedGame.Platform;
            videoGame.Developer = updatedGame.Developer;
            videoGame.Publisher = updatedGame.Publisher;

            return NoContent();
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteVideoGame(int id)
        {
            var videoGame = videoGames.FirstOrDefault(g => g.Id == id);
            if (videoGame is null)
                return NotFound();

            videoGames.Remove(videoGame);
            return NoContent();
        }
    }
}
