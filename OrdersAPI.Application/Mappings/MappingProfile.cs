using AutoMapper;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Domain.Entities;
using OrdersAPI.Domain.Enums;

namespace OrdersAPI.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Orders
        CreateMap<Order, OrderDto>()
            .ForMember(d => d.WaiterName, o => o.MapFrom(s => s.Waiter.FullName))
            .ForMember(d => d.TableNumber, o => o.MapFrom(s => s.Table != null ? s.Table.TableNumber : null));

        CreateMap<OrderItem, OrderItemDto>()
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.Name))
            .ForMember(d => d.PreparationLocation, o => o.MapFrom(s => s.Product.Location.ToString()))
            .ForMember(d => d.SelectedAccompaniments, o => o.MapFrom(s => s.OrderItemAccompaniments));

        // Products
        CreateMap<Product, ProductDto>()
            .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category.Name))
            .ForMember(d => d.PreparationLocation, o => o.MapFrom(s => s.Location.ToString()))
            .ForMember(d => d.Ingredients, o => o.MapFrom(s => s.ProductIngredients));

        CreateMap<ProductIngredient, ProductIngredientDto>()
            .ForMember(d => d.StoreProductName, o => o.MapFrom(s => s.StoreProduct.Name))
            .ForMember(d => d.Unit, o => o.MapFrom(s => s.StoreProduct.Unit));

        // Categories
        CreateMap<Category, CategoryDto>()
            .ForMember(d => d.ProductCount, o => o.MapFrom(s => s.Products.Count));

        // Users
        CreateMap<User, UserDto>()
            .ForMember(d => d.Role, o => o.MapFrom(s => s.Role.ToString()));

        // Tables
        CreateMap<CafeTable, TableDto>()
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.CurrentOrderId, o => o.MapFrom(s => 
                s.Orders.FirstOrDefault(order => 
                    order.Status != OrderStatus.Completed && 
                    order.Status != OrderStatus.Cancelled) != null 
                ? s.Orders.FirstOrDefault(order => 
                    order.Status != OrderStatus.Completed && 
                    order.Status != OrderStatus.Cancelled)!.Id 
                : (Guid?)null));

        // Inventory
        CreateMap<StoreProduct, StoreProductDto>()
            .ForMember(d => d.StoreName, o => o.MapFrom(s => s.Store.Name))
            .ForMember(d => d.IsLowStock, o => o.MapFrom(s => s.CurrentStock < s.MinimumStock));

        CreateMap<InventoryLog, InventoryLogDto>()
            .ForMember(d => d.StoreProductName, o => o.MapFrom(s => s.StoreProduct.Name))
            .ForMember(d => d.Type, o => o.MapFrom(s => s.Type.ToString()));

        CreateMap<Store, StoreDto>()
            .ForMember(d => d.ProductCount, o => o.MapFrom(s => s.StoreProducts.Count));

        // Procurement
        CreateMap<ProcurementOrder, ProcurementOrderDto>()
            .ForMember(d => d.StoreName, o => o.MapFrom(s => s.Store.Name))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()));

        CreateMap<ProcurementOrderItem, ProcurementOrderItemDto>()
            .ForMember(d => d.StoreProductName, o => o.MapFrom(s => s.StoreProduct.Name));

        // Notifications
        CreateMap<Notification, NotificationDto>()
            .ForMember(d => d.Type, o => o.MapFrom(s => s.Type.ToString()));

        // Statistics
        CreateMap<Product, TopProductDto>()
            .ForMember(d => d.ProductId, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Name))
            .ForMember(d => d.QuantitySold, o => o.Ignore())
            .ForMember(d => d.Revenue, o => o.Ignore());

        CreateMap<User, WaiterPerformanceDto>()
            .ForMember(d => d.WaiterId, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.WaiterName, o => o.MapFrom(s => s.FullName))
            .ForMember(d => d.TotalOrders, o => o.Ignore())
            .ForMember(d => d.TotalRevenue, o => o.Ignore())
            .ForMember(d => d.AverageOrderValue, o => o.Ignore());

        // Accompaniments
        CreateMap<AccompanimentGroup, AccompanimentGroupDto>()
            .ForMember(d => d.SelectionType, o => o.MapFrom(s => s.SelectionType.ToString()))
            .ForMember(d => d.Accompaniments, o => o.MapFrom(s => s.Accompaniments.OrderBy(a => a.DisplayOrder)));

        CreateMap<CreateAccompanimentGroupDto, AccompanimentGroup>()
            .ForMember(d => d.SelectionType, o => o.MapFrom(s => Enum.Parse<SelectionType>(s.SelectionType)))
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.Product, o => o.Ignore())
            .ForMember(d => d.Accompaniments, o => o.Ignore());

        CreateMap<UpdateAccompanimentGroupDto, AccompanimentGroup>()
            .ForMember(d => d.SelectionType, o => o.MapFrom(s => Enum.Parse<SelectionType>(s.SelectionType)))
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.ProductId, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.Product, o => o.Ignore())
            .ForMember(d => d.Accompaniments, o => o.Ignore());

        CreateMap<Accompaniment, AccompanimentDto>();

        CreateMap<CreateAccompanimentDto, Accompaniment>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.AccompanimentGroupId, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.AccompanimentGroup, o => o.Ignore())
            .ForMember(d => d.OrderItemAccompaniments, o => o.Ignore());

        CreateMap<UpdateAccompanimentDto, Accompaniment>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.AccompanimentGroupId, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.AccompanimentGroup, o => o.Ignore())
            .ForMember(d => d.OrderItemAccompaniments, o => o.Ignore());

        CreateMap<OrderItemAccompaniment, SelectedAccompanimentDto>()
            .ForMember(d => d.AccompanimentId, o => o.MapFrom(s => s.AccompanimentId))
            .ForMember(d => d.Name, o => o.MapFrom(s => s.Accompaniment.Name))
            .ForMember(d => d.ExtraCharge, o => o.MapFrom(s => s.PriceAtOrder));
    }
}