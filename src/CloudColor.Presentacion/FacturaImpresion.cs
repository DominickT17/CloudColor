using System.Drawing.Printing;
using CloudColor.Entidades;
namespace CloudColor.Presentacion;

public static class FacturaImpresion
{
    public static List<string> Lineas(Factura f,IEnumerable<LineaVenta> detalle,IEnumerable<Pago> pagos)
    {
        var lineas=new List<string> { "CLOUDCOLOR", "Factura: "+f.NumeroFactura, "Fecha: "+f.Fecha.ToString("dd/MM/yyyy HH:mm"), "Estado: "+f.Estado };
        void Texto(string texto) { for(int i=0;i<texto.Length;i+=85) lineas.Add(texto.Substring(i,Math.Min(85,texto.Length-i))); }
        Texto("Cliente: "+f.Cliente); Texto("NIT: "+(f.NIT ?? "Sin NIT")); Texto("Empleado emisor: "+f.Emisor);
        lineas.Add(new string('-',85)); lineas.Add("DETALLE");
        foreach(var d in detalle)
        {
            Texto(d.CodigoSKU+" | "+d.Producto+" | "+d.Presentacion+" | "+(d.Color ?? "Sin color"));
            Texto($"  Cantidad {d.Cantidad:0.####}   Precio Q {d.PrecioUnitario:N2}   Subtotal Q {d.Subtotal:N2}");
        }
        lineas.Add(new string('-',85)); lineas.Add($"TOTAL Q {f.TotalActual:N2}"); lineas.Add("MEDIOS DE PAGO");
        var lista=pagos.ToList();
        if(lista.Count==0) lineas.Add(f.TotalActual==0 ? "Sin importe por cobrar." : "Pendiente de cobro.");
        foreach(var p in lista) Texto($"{p.Medio}: Q {p.Monto:N2}   Referencia: {p.Referencia ?? "-"}");
        return lineas;
    }
    // El mismo renderizador sirve a PrintDocument y a las pruebas sin impresora fisica.
    public static int DibujarPagina(Graphics g,Rectangle area,IReadOnlyList<string> lineas,int inicio,int pagina)
    {
        using var fuente=new Font("Courier New",9); float alto=fuente.GetHeight(g)+4;
        float y=area.Top; int i=inicio;
        while(i<lineas.Count && y+alto<area.Bottom-alto)
        { g.DrawString(lineas[i++],fuente,Brushes.Black,area.Left,y); y+=alto; }
        g.DrawString("CloudColor · Página "+pagina,fuente,Brushes.Black,area.Left,area.Bottom-alto);
        return i;
    }
    public static PrintDocument Crear(Factura factura,IEnumerable<LineaVenta> detalle,IEnumerable<Pago> pagos)
    {
        var lineas=Lineas(factura,detalle,pagos); var doc=new PrintDocument { DocumentName="CloudColor "+factura.NumeroFactura };
        doc.DefaultPageSettings.Margins=new Margins(45,45,45,45);
        int indice=0,pagina=0;
        doc.BeginPrint+=(_,_)=> { indice=0; pagina=0; };
        doc.PrintPage+=(_,e)=> { indice=DibujarPagina(e.Graphics!,e.MarginBounds,lineas,indice,++pagina); e.HasMorePages=indice<lineas.Count; };
        return doc;
    }
}
