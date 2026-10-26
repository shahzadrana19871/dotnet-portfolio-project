namespace SapPosEtl.Worker;

public static class SalesValidator
{
    public static IReadOnlyList<string> Validate(PosSale sale)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(sale.ReceiptId))
            errors.Add("Receipt id is missing.");

        if (sale.Lines.Count == 0)
            errors.Add("Sale has no lines.");

        for (var i = 0; i < sale.Lines.Count; i++)
        {
            var line = sale.Lines[i];
            if (string.IsNullOrWhiteSpace(line.ItemCode))
                errors.Add($"Line {i + 1}: item code is missing.");
            if (line.Quantity <= 0)
                errors.Add($"Line {i + 1}: quantity must be greater than zero.");
            if (line.UnitPrice < 0)
                errors.Add($"Line {i + 1}: unit price cannot be negative.");
        }

        return errors;
    }
}
