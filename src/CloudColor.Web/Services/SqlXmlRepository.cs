using System.Collections.ObjectModel;
using System.Data;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Data.SqlClient;

namespace CloudColor.Web.Services;

public sealed class SqlXmlRepository(string connectionString) : IXmlRepository
{
    public IReadOnlyCollection<XElement> GetAllElements()
    {
        using var connection = new SqlConnection(connectionString);
        connection.Open();
        using var command = new SqlCommand("SELECT Xml FROM dbo.AspNetDataProtectionKeys ORDER BY Id", connection);
        using var reader = command.ExecuteReader();
        var keys = new List<XElement>();
        while (reader.Read()) keys.Add(XElement.Parse(reader.GetString(0), LoadOptions.PreserveWhitespace));
        return new ReadOnlyCollection<XElement>(keys);
    }

    public void StoreElement(XElement element, string friendlyName)
    {
        using var connection = new SqlConnection(connectionString);
        connection.Open();
        using var command = new SqlCommand("INSERT INTO dbo.AspNetDataProtectionKeys (FriendlyName, Xml) VALUES (@name, @xml)", connection);
        command.Parameters.Add("@name", SqlDbType.NVarChar, 256).Value = friendlyName;
        command.Parameters.Add("@xml", SqlDbType.NVarChar, -1).Value = element.ToString(SaveOptions.DisableFormatting);
        command.ExecuteNonQuery();
    }
}
