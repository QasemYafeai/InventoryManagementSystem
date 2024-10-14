namespace CAAMarketing.ViewModels
{
    public class PrintableViewModel
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public long UPC { get; set; }
        public int Quantity { get; set; }
        public string BarcodeSvg { get; set; }
    }

}
