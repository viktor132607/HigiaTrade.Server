using HygiaTrade.Domain.Payments;
using Microsoft.Extensions.Options;
using HygiaTrade.API.Services;
using HygiaTrade.Common.Options;
using HygiaTrade.Data.Interfaces;
using HygiaTrade.Data.Repositories;
using HygiaTrade.Domain.Interfaces;
using HygiaTrade.Domain.Services;

namespace HygiaTrade.API.ServiceExtensions;

public static class ServiceExtension
{
	public static IServiceCollection AddCustomServices(
		this IServiceCollection services)
	{
        services.AddScoped<IStripeOrderRepository, StripeOrderRepository>();
        services.AddScoped<IStripeGateway, StripeGateway>();
        services.AddScoped<StripeCheckoutService>();
        services.AddHostedService<StripeReconciliationWorker>();
		// SERVICES
		services.AddTransient<IAuthService, AuthService>();
		services.AddTransient<IUserService, UserService>();
		services.AddTransient<ICategoryService, CategoryService>();
		services.AddTransient<IBrandService, BrandService>();
		services.AddTransient<IBrandReadService, BrandReadService>();
		services.AddTransient<IBrandMutationService, BrandMutationService>();
		services.AddScoped<IBrandRepository, BrandRepository>();
		services.AddSingleton<IBrandPolicy, BrandPolicy>();
		services.AddSingleton<IBrandMapper, BrandMapper>();
		services.AddSingleton<IBrandClock, BrandClock>();
		services.AddTransient<IContactService, ContactService>();
		services.AddTransient<IDistributionRouteService, DistributionRouteService>();
		services.AddScoped<IDistributionRouteRepository, DistributionRouteRepository>();
		services.AddTransient<IDistributionRouteGeocoder, DistributionRouteGeocoder>();
		services.AddTransient<IDistributionRoadRouter, DistributionRoadRouter>();
		services.AddTransient<IDistributionRouteOptimizer, DistributionRouteOptimizer>();
		services.AddTransient<IDistributionRouteDelay, DistributionRouteDelay>();
		services.AddTransient<IInvoiceImportService, InvoiceImportService>();
		services.AddTransient<IInvoiceTextExtractor, InvoiceTextExtractor>();
		services.AddTransient<IInvoiceProcessRunner, InvoiceProcessRunner>();
		services.AddTransient<IInvoiceParser, InvoiceParser>();
		services.AddScoped<IInvoiceImportRepository, InvoiceImportRepository>();
		services.AddTransient<IInvoiceNumberService, InvoiceNumberService>();
		services.AddTransient<IReportsService, ReportsService>();
		services.AddSingleton<IReportsRequestNormalizer, ReportsRequestNormalizer>();
		services.AddScoped<IReportsDataSource, ReportsDataSource>();
		services.AddScoped<IStockEntryReportReader, StockEntryReportReader>();
		services.AddSingleton<IAdminReportBuilder, AdminReportBuilder>();
		services.AddTransient<IHomeSlideshowService, HomeSlideshowService>();
		services.AddScoped<IHomeSlideshowStore, HomeSlideshowStore>();
		services.AddSingleton<IHomeSlideshowPayloadNormalizer, HomeSlideshowPayloadNormalizer>();
		services.AddSingleton<IHomeSlideshowDefaults, HomeSlideshowDefaults>();
		services.AddTransient<IProductImagePresentationService, ProductImagePresentationService>();
		services.AddTransient<INewProductsService, NewProductsService>();
		services.AddScoped<INewProductStatusRepository, NewProductStatusRepository>();
		services.AddSingleton<INewProductsPolicy, NewProductsPolicy>();
		services.AddSingleton<INewProductsClock, NewProductsClock>();
		services.AddTransient<IInventoryService, InventoryService>();
		services.AddScoped<IInventoryReader, InventoryReader>();
		services.AddScoped<IInventoryStockAdder, InventoryStockAdder>();
		services.AddSingleton<IInventoryRequestValidator, InventoryRequestValidator>();
		services.AddSingleton<IInventoryClock, InventoryClock>();
		services.AddTransient<IStoredImageService, StoredImageService>();
		services.AddTransient<ITranslationService, TranslationService>();
		services.AddTransient<IProductService, ProductService>();
		services.AddTransient<IProductReadService, ProductReadService>();
		services.AddTransient<IProductMutationService, ProductMutationService>();
		services.AddTransient<IProductSearchService, ProductSearchService>();
		services.AddTransient<IProductImageService, ProductImageService>();
		services.AddTransient<IProductPricingPolicy, ProductPricingPolicy>();
		services.AddTransient<IWishlistService, WishlistService>();
		services.AddTransient<IReviewService, ReviewService>();
		services.AddTransient<IOrderService, OrderService>();
		services.AddTransient<IOrderAdministrationService, OrderAdministrationService>();
		services.AddTransient<IOrderCartService, OrderCartService>();
		services.AddTransient<ICurrentOrderCheckoutService, CurrentOrderCheckoutService>();
		services.AddTransient<IGuestOrderCheckoutService, GuestOrderCheckoutService>();
		services.AddTransient<IOrderPricingService, OrderPricingService>();
		services.AddTransient<IOrderStockService, OrderStockService>();
		services.AddTransient<IOrderPaymentMethodResolver, OrderPaymentMethodResolver>();
		services.AddTransient<IGdprService, GdprService>();

		services.AddSingleton<
			IPasswordResetTokenStore,
			MemoryPasswordResetTokenStore>();

		services.AddTransient<ConsoleEmailNotificationService>();

		services.AddHttpClient<IResendEmailTransport, ResendEmailTransport>();
		services.AddTransient<IEmailNotificationTemplateBuilder, EmailNotificationTemplateBuilder>();
		services.AddTransient<ResendEmailNotificationService>();

		services.AddTransient<IEmailNotificationService>(
			serviceProvider =>
			{
				EmailOptions options = serviceProvider
					.GetRequiredService<IOptions<EmailOptions>>()
					.Value;

				if (options.DeliveryMode.Equals(
					"Resend",
					StringComparison.OrdinalIgnoreCase))
				{
					return serviceProvider
						.GetRequiredService<ResendEmailNotificationService>();
				}

				return serviceProvider
					.GetRequiredService<ConsoleEmailNotificationService>();
			});

		// REPOSITORIES
		services.AddScoped<IUserRepository, UserRepository>();
		services.AddScoped<ICategoryRepository, CategoryRepository>();
		services.AddScoped<IProductRepository, ProductRepository>();
		services.AddScoped<IWishlistRepository, WishlistRepository>();
		services.AddScoped<IReviewRepository, ReviewRepository>();
		services.AddScoped<IImageRepository, ImageRepository>();
		services.AddScoped<IOrderRepository, OrderRepository>();
		services.AddScoped<IOrderItemRepository, OrderItemRepository>();
		services.AddScoped<IGuestOrderRepository, GuestOrderRepository>();

		return services;
	}
}
