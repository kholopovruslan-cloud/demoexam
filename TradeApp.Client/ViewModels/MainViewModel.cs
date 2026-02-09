using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TradeApp.Client.Models;
using TradeApp.Client.Services;
using TradeApp.Client.Views;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace TradeApp.Client.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ApiService _apiService = new();
    private User? _currentUser;

    [ObservableProperty] private ObservableCollection<Product> products = new();
    [ObservableProperty] private ObservableCollection<Category> categories = new();
    [ObservableProperty] private ObservableCollection<Order> orders = new();
    [ObservableProperty] private ObservableCollection<Address> addresses = new();
    [ObservableProperty] private ObservableCollection<Status> statuses = new();
    [ObservableProperty] private Product? selectedProduct;
    [ObservableProperty] private Order? selectedOrder;
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private Category? selectedCategory;
    [ObservableProperty] private string sortBy = "name";
    [ObservableProperty] private bool ascending = true;
    [ObservableProperty] private bool canEditProducts;
    [ObservableProperty] private bool canManageOrders;
    [ObservableProperty] private bool canSearchAndFilter = true;
    [ObservableProperty] private bool canViewOrdersList = true;
    [ObservableProperty] private int selectedTabIndex;

    public MainViewModel()
    {
        _currentUser = App.Current.Properties["CurrentUser"] as User;
        if (_currentUser != null)
        {
            // По ТЗ: Админ (1) — CRUD товары и заказы. Менеджер (2) — просмотр товаров с фильтром, просмотр заказов. Клиент (3) и Гость (4) — только просмотр товаров, заказы по ТЗ не предусмотрены.
            var isAdmin = _currentUser.RoleId == 1;
            var isManager = _currentUser.RoleId == 2;
            CanEditProducts = isAdmin;
            CanManageOrders = isAdmin;
            CanSearchAndFilter = isAdmin || isManager;
            CanViewOrdersList = isAdmin || isManager;
        }
        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        await LoadProductsAsync();
        await LoadCategoriesAsync();
        if (CanViewOrdersList)
        {
            await LoadOrdersAsync();
            await LoadAddressesAsync();
            await LoadStatusesAsync();
        }
    }

    [RelayCommand]
    private async Task LoadProductsAsync()
    {
        var q = $"?sortBy={SortBy}&ascending={Ascending}";
        if (!string.IsNullOrWhiteSpace(SearchText)) q += $"&search={Uri.EscapeDataString(SearchText)}";
        if (SelectedCategory != null && SelectedCategory.Id != 0) q += $"&categoryId={SelectedCategory.Id}";
        var r = await _apiService.GetAsync<List<Product>>($"Products{q}");
        if (r != null) { Products.Clear(); foreach (var p in r) Products.Add(p); }
    }

    [RelayCommand]
    private async Task LoadCategoriesAsync()
    {
        var r = await _apiService.GetAsync<List<Category>>("Categories");
        if (r != null)
        {
            Categories.Clear();
            var allCat = new Category { Id = 0, Name = "Все категории" };
            Categories.Add(allCat);
            foreach (var c in r) Categories.Add(c);
            if (SelectedCategory == null) SelectedCategory = allCat;
        }
    }

    [RelayCommand]
    private async Task LoadOrdersAsync()
    {
        // По ТЗ: админ и менеджер видят все заказы; гостю и клиенту доступен только просмотр товаров, список заказов не показывается
        var showAll = _currentUser?.RoleId == 1 || _currentUser?.RoleId == 2;
        var q = showAll ? "" : $"?userId={_currentUser?.UserId}";
        var r = await _apiService.GetAsync<List<Order>>($"Orders{q}");
        if (r != null) { Orders.Clear(); foreach (var o in r) Orders.Add(o); }
    }

    [RelayCommand]
    private async Task LoadAddressesAsync()
    {
        var r = await _apiService.GetAsync<List<Address>>("Addresses");
        if (r != null) { Addresses.Clear(); foreach (var a in r) Addresses.Add(a); }
    }

    [RelayCommand]
    private async Task LoadStatusesAsync()
    {
        var r = await _apiService.GetAsync<List<Status>>("Statuses");
        if (r != null) { Statuses.Clear(); foreach (var s in r) Statuses.Add(s); }
    }

    [RelayCommand]
    private async Task CreateOrderAsync()
    {
        // В форму заказа передаём все товары (без фильтра), чтобы можно было добавить любой товар
        var allProducts = await _apiService.GetAsync<List<Product>>("Products?sortBy=name&ascending=true");
        var productList = (allProducts != null && allProducts.Count > 0) ? allProducts : Products.ToList();
        var d = new OrderCreateWindow(productList, Addresses.ToList());
        if (d.ShowDialog() != true || d.OrderItems == null || d.OrderItems.Count == 0) return;
        var userId = _currentUser?.UserId ?? 0;
        if (userId <= 0) { MessageBox.Show("Для создания заказа необходимо войти в систему.", "Внимание"); return; }
        var request = new CreateOrderRequestDto { AddressId = d.SelectedAddressId, Items = d.OrderItems };
        Order? created = null;
        try { created = await _apiService.PostAsync<Order>($"Orders?userId={userId}", request); }
        catch (System.Net.Http.HttpRequestException ex) { MessageBox.Show("Ошибка при создании заказа: " + (ex.InnerException?.Message ?? ex.Message), "Ошибка"); return; }
        if (created != null) { _ = LoadOrdersAsync(); MessageBox.Show("Заказ создан", "Успех"); }
        else MessageBox.Show("Не удалось создать заказ.", "Ошибка");
    }

    [RelayCommand]
    private async Task ChangeOrderStatusAsync()
    {
        if (SelectedOrder == null) { MessageBox.Show("Выберите заказ.", "Внимание"); return; }
        var d = new OrderStatusWindow(Statuses.ToList(), SelectedOrder.StatusId);
        if (d.ShowDialog() != true) return;
        var ok = await _apiService.PutAsync($"Orders/{SelectedOrder.Id}/status?userId={_currentUser?.UserId ?? 0}", new { StatusId = d.SelectedStatusId });
        if (ok) { _ = LoadOrdersAsync(); MessageBox.Show("Статус обновлён.", "Успех"); }
        else MessageBox.Show("Не удалось изменить статус.", "Ошибка");
    }

    [RelayCommand]
    private async Task AddProductAsync()
    {
        var realCategories = Categories.Where(c => c.Id != 0).ToList();
        var product = new Product { CategoryId = realCategories.Count > 0 ? realCategories[0].Id : 1 };
        var d = new ProductEditWindow(product, Categories);
        if (d.ShowDialog() != true) return;
        var url = $"Products?userId={_currentUser?.UserId ?? 0}";
        var body = new { d.Product.Name, d.Product.Description, d.Product.Price, d.Product.Quantity, d.Product.CategoryId, d.Product.ManufacturerId, d.Product.SupplierId, d.Product.ImageUrl };
        Product? created = null;
        try { created = await _apiService.PostAsync<Product>(url, body); } catch (System.Net.Http.HttpRequestException ex) { MessageBox.Show("Ошибка при добавлении: " + (ex.Message ?? ex.InnerException?.Message ?? "400/500"), "Ошибка"); return; }
        if (created != null) _ = LoadProductsAsync();
        else MessageBox.Show("Не удалось добавить товар. Проверьте подключение к серверу и данные.", "Ошибка");
    }

    [RelayCommand]
    private async Task EditProductAsync()
    {
        if (SelectedProduct == null) return;
        var d = new ProductEditWindow(SelectedProduct, Categories);
        if (d.ShowDialog() != true) return;
        var ok = await _apiService.PutAsync($"Products/{d.Product.Id}?userId={_currentUser?.UserId ?? 0}", d.Product);
        if (ok) _ = LoadProductsAsync();
        else MessageBox.Show("Не удалось сохранить изменения. Проверьте подключение к серверу.", "Ошибка");
    }

    [RelayCommand]
    private async Task DeleteProductAsync()
    {
        if (SelectedProduct == null) return;
        if (MessageBox.Show("Удалить товар?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
        {
            if (await _apiService.DeleteAsync($"Products/{SelectedProduct.Id}?userId={_currentUser?.UserId}")) await LoadProductsAsync();
        }
    }

    [RelayCommand]
    private async Task ExportProductsCsvAsync()
    {
        try
        {
            var d = new SaveFileDialog { Filter = "CSV (*.csv)|*.csv|Excel (*.xlsx)|*.xlsx", FileName = "products.csv" };
            if (d.ShowDialog() != true) return;
            if (d.FilterIndex == 2) new ExportService().ExportProductsToExcel(Products.ToList(), d.FileName);
            else
            {
                var csv = new System.Text.StringBuilder();
                csv.AppendLine("Id,Название,Описание,Цена,Количество,Категория,Производитель");
                foreach (var p in Products) csv.AppendLine($"{p.Id},\"{p.Name}\",\"{p.Description}\",{p.Price},{p.Quantity},\"{p.Category?.Name}\",\"{p.Manufacturer?.Name}\"");
                await File.WriteAllTextAsync(d.FileName, csv.ToString(), System.Text.Encoding.UTF8);
            }
            MessageBox.Show("Экспорт завершен", "Успех");
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка"); }
    }

    [RelayCommand]
    private void SelectProductsTab() => SelectedTabIndex = 0;

    [RelayCommand]
    private void SelectOrdersTab() => SelectedTabIndex = 1;

    [RelayCommand]
    private void Logout()
    {
        App.Current.Properties.Remove("CurrentUser");
        new LoginWindow().Show();
        Application.Current.Windows.OfType<MainWindow>().First().Close();
    }

    partial void OnSearchTextChanged(string value) => _ = LoadProductsAsync();
    partial void OnSelectedCategoryChanged(Category? value) => _ = LoadProductsAsync();
    partial void OnAscendingChanged(bool value) => _ = LoadProductsAsync();
    partial void OnSortByChanged(string value) => _ = LoadProductsAsync();
}
