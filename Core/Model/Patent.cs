using System.ComponentModel.DataAnnotations;

namespace Core.Model
{
	public class Patent : Entity
	{
		[MaxLength]
		public string Html { get; set; }
		public string PatentId { get; set; }
		public string InventorField { get; set; }
		public int Year { get; set; }
	}
}
