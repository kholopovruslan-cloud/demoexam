using ClosedXML.Excel;
using TradeApp.Client.Models;

namespace TradeApp.Client.Services;

public class ExportService
{
    public void ExportProductsToExcel(List<Product> products, string filePath)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Товары");
        ws.Cell(1, 1).Value = "ID"; ws.Cell(1, 2).Value = "Название"; ws.Cell(1, 3).Value = "Описание"; ws.Cell(1, 4).Value = "Цена"; ws.Cell(1, 5).Value = "Количество"; ws.Cell(1, 6).Value = "Категория"; ws.Cell(1, 7).Value = "Производитель";
        int row = 2;
        foreach (var p in products) { ws.Cell(row, 1).Value = p.Id; ws.Cell(row, 2).Value = p.Name; ws.Cell(row, 3).Value = p.Description; ws.Cell(row, 4).Value = p.Price; ws.Cell(row, 5).Value = p.Quantity; ws.Cell(row, 6).Value = p.Category?.Name; ws.Cell(row, 7).Value = p.Manufacturer?.Name; row++; }
        ws.Columns().AdjustToContents();
        wb.SaveAs(filePath);
    }

    public void ExportOrdersToExcel(List<Order> orders, string filePath)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Заказы");
        ws.Cell(1, 1).Value = "ID"; ws.Cell(1, 2).Value = "Пользователь"; ws.Cell(1, 3).Value = "Статус"; ws.Cell(1, 4).Value = "Дата"; ws.Cell(1, 5).Value = "Сумма";
        int row = 2;
        foreach (var o in orders) { ws.Cell(row, 1).Value = o.Id; ws.Cell(row, 2).Value = o.User?.FullName; ws.Cell(row, 3).Value = o.Status?.Name; ws.Cell(row, 4).Value = o.CreatedAt; ws.Cell(row, 5).Value = o.TotalSum; row++; }
        ws.Columns().AdjustToContents();
        wb.SaveAs(filePath);
    }
}
