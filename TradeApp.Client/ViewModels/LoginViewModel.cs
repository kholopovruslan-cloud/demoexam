using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TradeApp.Client.Models;
using TradeApp.Client.Services;
using TradeApp.Client.Views;
using System.Windows;

namespace TradeApp.Client.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly ApiService _apiService = new();

    [ObservableProperty] private string login = string.Empty;
    [ObservableProperty] private string password = string.Empty;
    [ObservableProperty] private string errorMessage = string.Empty;

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Login) || string.IsNullOrWhiteSpace(Password)) { ErrorMessage = "Введите логин и пароль"; return; }
        try
        {
            var result = await _apiService.PostAsync<User>("Auth/login", new { Login, Password });
            if (result != null)
            {
                App.Current.Properties["CurrentUser"] = result;
                new MainWindow().Show();
                Application.Current.Windows.OfType<LoginWindow>().First().Close();
            }
            else ErrorMessage = "Неверный логин или пароль";
        }
        catch (HttpRequestException ex)
        {
            var inner = ex.InnerException?.Message ?? ex.Message;
            if (inner.Contains("500"))
                ErrorMessage = "Сервер вернул ошибку 500 (ошибка на сервере). Часто это из-за того, что PostgreSQL требует пароль. Укажите пароль в TradeApp.Server/appsettings.Development.json в секции Postgres:Password или задайте переменную POSTGRES_PASSWORD. Подробнее: Docs/Ошибка_500_и_пароль_БД.md";
            else
                ErrorMessage = "Сервер недоступен (API не отвечает). Запустите проект TradeApp.Server (F5), дождитесь в консоли «Now listening on: http://127.0.0.1:5000», затем нажмите Войти снова. Подробность: " + inner;
        }
        catch (TaskCanceledException)
        {
            ErrorMessage = "Таймаут. Сервер не ответил. Запустите TradeApp.Server и повторите попытку.";
        }
        catch (Exception ex) { ErrorMessage = $"Ошибка: {ex.Message}"; }
    }

    /// <summary>Войти как гость без пароля — по ТЗ возможность перейти на экран просмотра товаров в роли гостя.</summary>
    [RelayCommand]
    private async Task GuestLoginAsync()
    {
        try
        {
            var result = await _apiService.PostAsync<User>("Auth/guest", null);
            if (result != null)
            {
                App.Current.Properties["CurrentUser"] = result;
                new MainWindow().Show();
                Application.Current.Windows.OfType<LoginWindow>().First().Close();
            }
            else ErrorMessage = "Не удалось войти как гость.";
        }
        catch (HttpRequestException ex)
        {
            var inner = ex.InnerException?.Message ?? ex.Message;
            if (inner.Contains("500"))
                ErrorMessage = "Ошибка сервера. Проверьте подключение к БД (Docs/Ошибка_500_и_пароль_БД.md).";
            else ErrorMessage = "Сервер недоступен. Запустите TradeApp.Server (F5). " + inner;
        }
        catch (Exception ex) { ErrorMessage = $"Ошибка: {ex.Message}"; }
    }
}
