namespace Commerce.Models;

public record Product
{
	public int ProductId { get; init; }
	public string? Brand { get; init; }
	public string? Name { get; init; }
	public string? LongName { get; init; }
	public string? Description { get; init; }
	public string? Category { get; init; }
	public string? FullPrice { get; init; }
	public string? Price { get; init; }
	public string? Discount { get; init; }
	public string? Photo { get; init; }
	public double? Rating { get; init; }

	public string? DiscountedPrice => Price;

	public Review[]? Reviews { get; init; }

}
