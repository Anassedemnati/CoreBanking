using CoreBanking.Products.Application.Products;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace CoreBanking.Products.Api.Controllers;

/// <summary>
/// Manages savings product templates.
/// </summary>
/// <remarks>
/// A savings product is a template that defines the interest calculation settings and
/// currency for a category of savings accounts. Individual savings accounts are always
/// linked to one product and inherit its currency and default interest rate at the time
/// of submission.
///
/// Products can be in <c>Active</c> or <c>Inactive</c> status. Only <c>Active</c> products
/// can be used when opening new savings account applications.
///
/// Corresponds to Fineract <c>/v1/savingsproducts</c>.
/// </remarks>
[ApiController]
[Route("api/v1/savingsproducts")]
[Produces("application/json")]
[Consumes("application/json")]
public sealed class SavingsProductsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Create a new savings product.
    /// </summary>
    /// <remarks>
    /// Defines a new savings product template that savings accounts can be based on.
    /// The product is created in <c>Active</c> status.
    ///
    /// **Mandatory fields:**
    /// - <c>name</c> — unique product name (max 100 characters).
    /// - <c>currencyCode</c> — ISO 4217 three-letter currency code (e.g. <c>USD</c>, <c>EUR</c>, <c>MAD</c>).
    /// - <c>currencyDecimalPlaces</c> — number of decimal places for monetary amounts (0–4).
    /// - <c>nominalAnnualInterestRate</c> — annual rate as a decimal percentage (e.g. <c>3.5</c> for 3.5 %). Must be ≥ 0.
    ///
    /// Corresponds to Fineract <c>POST /v1/savingsproducts</c>.
    /// </remarks>
    /// <param name="cmd">Savings product creation payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The newly created savings product ID.</returns>
    /// <response code="201">Savings product created successfully. Location header points to the new resource.</response>
    /// <response code="400">Validation failed — mandatory fields are missing or values are out of range.</response>
    /// <response code="409">A savings product with the same name already exists.</response>
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateSavingsProductCommand cmd, CancellationToken ct)
    {
        var id = await mediator.Send(cmd, ct);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    /// <summary>
    /// Retrieve a savings product by ID.
    /// </summary>
    /// <remarks>
    /// Returns the full savings product definition including its current status, currency
    /// settings, and interest rate configuration.
    ///
    /// Corresponds to Fineract <c>GET /v1/savingsproducts/{productId}</c>.
    /// </remarks>
    /// <param name="id">Unique identifier of the savings product.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Savings product data transfer object.</returns>
    /// <response code="200">Savings product found and returned.</response>
    /// <response code="404">No savings product found with the given <c>id</c>.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SavingsProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var dto = await mediator.Send(new GetSavingsProductByIdQuery(id), ct);
        return Ok(dto);
    }

    /// <summary>
    /// List all savings products.
    /// </summary>
    /// <remarks>
    /// Returns all savings products regardless of status. Consumers should filter by
    /// <c>status</c> if only active products are needed (e.g. to populate a product
    /// selection dropdown when opening a new savings account).
    ///
    /// Corresponds to Fineract <c>GET /v1/savingsproducts</c>.
    /// </remarks>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of all savings product data transfer objects.</returns>
    /// <response code="200">List returned (may be empty).</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SavingsProductDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var dtos = await mediator.Send(new ListSavingsProductsQuery(), ct);
        return Ok(dtos);
    }
}
