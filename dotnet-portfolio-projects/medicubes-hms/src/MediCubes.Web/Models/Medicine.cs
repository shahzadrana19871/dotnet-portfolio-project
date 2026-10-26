using System.ComponentModel.DataAnnotations;

namespace MediCubes.Web.Models;

public class Medicine
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(40)]
    public string Sku { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int Quantity { get; set; }

    [Display(Name = "Reorder level"), Range(0, int.MaxValue)]
    public int ReorderLevel { get; set; }

    [Display(Name = "Unit price"), Range(0, 1_000_000)]
    [DataType(DataType.Currency)]
    public decimal UnitPrice { get; set; }

    [Display(Name = "Expiry date"), DataType(DataType.Date)]
    public DateTime ExpiryDate { get; set; } = DateTime.Today.AddYears(1);

    public bool IsLowStock => Quantity <= ReorderLevel;

    public bool IsExpired(DateTime today) => ExpiryDate.Date < today.Date;
}
