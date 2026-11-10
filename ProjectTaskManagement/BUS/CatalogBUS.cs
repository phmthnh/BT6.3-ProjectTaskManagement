using System.Data;
using System.Globalization;
using ProjectTaskManagement.DAL;
using ProjectTaskManagement.DTO;

namespace ProjectTaskManagement.BUS;

public sealed class CatalogBUS
{
    public CatalogDTO Catalog { get; }
    private readonly CatalogDAL dal;

    public CatalogBUS(string table)
    {
        Catalog = Definitions().Single(item => item.Table == table);
        dal = new CatalogDAL(Catalog);
    }

    public static CatalogDTO[] Definitions() => new[]
    {
        new CatalogDTO("DuAn", "Dự án", "MaDuAn", true, new[] { new FieldDTO("TenDuAn","Tên dự án","text",150,true), new FieldDTO("NgayBatDau","Ngày bắt đầu yyyy-MM-dd","date",10,false), new FieldDTO("NgayKetThuc","Ngày kết thúc yyyy-MM-dd","date",10,false), new FieldDTO("TrangThai","Trạng thái","text",50,false) })
    };

    public DataTable List(string term = "") => dal.List(term.Trim());
    public void Delete(object key) => dal.Delete(key);

    public void Save(Dictionary<string,object?> input, object? key)
    {
        var values = new Dictionary<string,object?>();
        foreach (var field in Catalog.Fields)
        {
            var value = Convert.ToString(input.GetValueOrDefault(field.Column))?.Trim() ?? "";
            if (value.Length == 0 && !field.Required) { values[field.Column] = null; continue; }
            if (value.Length == 0 || value.Length > field.Length)
                throw new ArgumentException($"Nhập {field.Caption} hợp lệ.");
            switch (field.Kind)
            {
                case "int":
                    if (!int.TryParse(value,out var count) || count < 0 || (field.Column=="MaKH" && count==0))
                        throw new ArgumentException($"{field.Caption} không hợp lệ.");
                    values[field.Column] = count;
                    break;
                case "money":
                    if (!decimal.TryParse(value,NumberStyles.AllowDecimalPoint,CultureInfo.CurrentCulture,out var money)
                        || money < 0 || money > 9999999999999999.99m || Math.Round(money,2)!=money)
                        throw new ArgumentException("Đơn giá không hợp lệ.");
                    values[field.Column] = money;
                    break;
                case "date":
                    if (!DateTime.TryParseExact(value,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out var date)
                        || date.Year < 1753) throw new ArgumentException("Ngày phải theo định dạng yyyy-MM-dd.");
                    values[field.Column] = date;
                    break;
                default:
                    values[field.Column] = field.Column == "BienSo" ? value.ToUpperInvariant() : value;
                    break;
            }
        }
        if (values.GetValueOrDefault("NgayBatDau") is DateTime start &&
            values.GetValueOrDefault("NgayKetThuc") is DateTime end && end < start)
            throw new ArgumentException("Ngày kết thúc phải từ ngày bắt đầu.");
        dal.Save(values,key);
    }
}
