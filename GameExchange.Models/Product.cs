using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace GameExchange.Models
{
    public class Product
    {
        public int Id { get; set; }
        [Required]
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        [Required]
        public string Developer { get; set; } = string.Empty;
        [Required]
        [Display(Name = "List Price")]
        [Range(1, 10000)]
        public double ListPrice { get; set; }

        [Required]
        [Display(Name = "Price for 1-50")]
        [Range(1, 10000)]
        public double Price { get; set; }

        [Required]
        [Display(Name = "Price for 51-100")]
        [Range(1, 10000)]
        public double Price50 { get; set; }

        [Required]
        [Display(Name ="Price for 100+")]
        [Range(1, 10000)]
        public double Price100 { get; set; }
		[ValidateNever]
		public string ImageUrl { get; set; } = string.Empty;
        [Required]
        [Display(Name = "Category")]
        public int CategoryId { get; set; } //becomes a foreign key to Categories table
        [ForeignKey("CategoryId")] //directly makes CategoryId a foreign key
        [ValidateNever]
        public Category Category { get; set; }

        [Required]
        [Display(Name ="Cover Type")]
        public int CoverTypeId { get; set; } //becomes a foreign key to Cover Types table
        [ForeignKey("CoverTypeId")] //directly makes CoverTypeId a foreign key
		[ValidateNever]
		public CoverType CoverType { get; set; }

        [Required]
        [Display(Name = "Quantity")]
        public int Qty { get; set; }
    }
}
