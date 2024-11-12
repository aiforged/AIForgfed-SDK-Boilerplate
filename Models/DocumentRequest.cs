namespace AIForgfed_Intergation_Boilerplate.Models
{
    public class DocumentRequest
    {
        public int DocId { get; set; }
        public int MasterID { get; set; }
        public int ProjectID { get; set; }
        public int DocumentID { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }
        public required string Filename { get; set; }
        public int Catagory { get; set; }
        public required string Status { get; set; }
        public required string Comment { get; set; }
        public required string Result { get; set; }
    }
}
