using HygiaTrade.API.Services;
using HygiaTrade.Core.StaticClasses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HygiaTrade.API.Controllers;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/distribution-routes")]
public sealed class DistributionRoutesController(
    IDistributionRouteService distributionRouteService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DistributionRoutesPageResponse>> GetAsync(
        CancellationToken cancellationToken)
    {
        DistributionRoutesPageResponse response =
            await distributionRouteService.GetAsync(cancellationToken);

        return Ok(response);
    }

    [HttpPost("optimize")]
    public async Task<ActionResult<DistributionRouteDto>> OptimizeAsync(
        [FromBody] CreateDistributionRouteRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            DistributionRouteDto route =
                await distributionRouteService.OptimizeAsync(request, cancellationToken);

            return Ok(route);
        }
        catch (DistributionRouteServiceException ex)
        {
            return StatusCode(ex.StatusCode, ex.Payload);
        }
    }

    [HttpDelete("{routeId:guid}")]
    public async Task<IActionResult> DeleteAsync(
        Guid routeId,
        CancellationToken cancellationToken)
    {
        try
        {
            await distributionRouteService.DeleteAsync(routeId, cancellationToken);
            return NoContent();
        }
        catch (DistributionRouteServiceException ex)
        {
            return StatusCode(ex.StatusCode, ex.Payload);
        }
    }
}

public sealed class CreateDistributionRouteRequest
{
    public string? DistributorName { get; set; }
    public DateOnly? RouteDate { get; set; }
    public List<Guid>? OrderIds { get; set; }
}

public sealed class DistributionRoutesPageResponse
{
    public DistributionDepotDto Depot { get; set; } = new();
    public List<DistributionRouteDto> Routes { get; set; } = [];
    public List<DistributionOrderDto> UnassignedOrders { get; set; } = [];
}

public sealed class DistributionRouteStore
{
    public List<DistributionRouteDto> Routes { get; set; } = [];
}

public sealed class DistributionRouteDto
{
    public Guid Id { get; set; }
    public string DistributorName { get; set; } = string.Empty;
    public DateOnly RouteDate { get; set; }
    public DateTime CreatedOn { get; set; }
    public DistributionDepotDto Start { get; set; } = new();
    public DistributionDepotDto End { get; set; } = new();
    public double TotalDistanceKm { get; set; }
    public int EstimatedDurationMinutes { get; set; }
    public string NavigationUrl { get; set; } = string.Empty;
    public List<DistributionRouteStopDto> Stops { get; set; } = [];
}

public sealed class DistributionDepotDto
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public sealed class DistributionOrderDto
{
    public Guid Id { get; set; }
    public string Names { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string FullAddress { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
    public int Status { get; set; }
    public decimal OrderTotalPrice { get; set; }
}

public sealed class DistributionRouteStopDto
{
    public Guid OrderId { get; set; }
    public int Position { get; set; }
    public string Names { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string FullAddress { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
    public decimal OrderTotalPrice { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string GeocodingPrecision { get; set; } = string.Empty;
}
