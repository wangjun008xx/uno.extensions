﻿namespace Commerce.ViewModels;

public partial class ProductDetailsViewModel
{
	private readonly IProductService _productService;
	private readonly Product _product;

	public ProductDetailsViewModel(
		IProductService productService,
		Product product)
	{
		_productService = productService;
		_product = product;
	}

	public virtual IFeed<Product> Product => Feed.Async(Load);

	public virtual IFeed<IImmutableList<Review>> Reviews => Product.SelectAsync(async (p, ct) => await _productService.GetReviews(p.ProductId, ct));

	private async ValueTask<Product> Load(CancellationToken ct)
		=> _product;
}
