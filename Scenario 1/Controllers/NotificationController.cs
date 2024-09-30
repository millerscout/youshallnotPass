using Microsoft.AspNetCore.Mvc;
using Scenario_1.Models;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class NotificationController : ControllerBase
{
    [HttpPost("send")]
    public async Task<IActionResult> SendNotification([FromBody] NotificationRequest request)
    {
        await Task.Yield();
        return Ok("Notification sent successfully.");
        
    }
}
