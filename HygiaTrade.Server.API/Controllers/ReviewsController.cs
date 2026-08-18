using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HygiaTrade.API.Helpers;
using HygiaTrade.Common.Requests.Review;
using HygiaTrade.Domain.Interfaces;

namespace HygiaTrade.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController(IReviewService reviewService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> SearchAsync([FromQuery] SearchReviewsRequest request)
    {
        return await ControllerProcessor.ProcessAsync(() => reviewService.SearchReviewsAsync(request), this, true);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] CreateReviewRequest request)
    {
        return await ControllerProcessor.ProcessAsync(() => reviewService.CreateAsync(request), this, true);
    }
    
    [Authorize]
    [HttpPut]
    public async Task<IActionResult> UpdateAsync([FromBody] UpdateReviewRequest request)
    {
        return await ControllerProcessor.ProcessAsync(() => reviewService.UpdateAsync(request), this, true);
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsync(Guid id)
    {
        return await ControllerProcessor.ProcessAsync<object>(
            async () => await reviewService.DeleteAsync(id), this);
    }
}
