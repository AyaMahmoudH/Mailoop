﻿using System.ComponentModel.DataAnnotations.Schema;

namespace Mailo.Models
{
    public class ColorImage
    {
        public int Id { get; set; }
        public int ColorId { get; set; }
        public Color? Color { get; set; }
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        public string? ImageUrl { get; set; }
        [NotMapped]
        public IFormFile? clientFile { get; set; }
        public byte[]? dbImage { get; set; }
        [NotMapped]
        public string? imageSrc
        {
            get
            {
                if (dbImage != null)
                {
                    string base64String = Convert.ToBase64String(dbImage, 0, dbImage.Length);
                    return "data:image/jpg;base64," + base64String;
                }
                else
                {
                    return string.Empty;
                }
            }
            set
            {
            }
        }
     


    }
}
