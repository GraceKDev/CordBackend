using System.ComponentModel.DataAnnotations.Schema;

namespace CordBackend.Models;
[Table("routes")]
public class Route
{
    [Column("id")]
    public int Id {get;set;}
    [Column("name")]
    public string Name {get;set;} = string.Empty;
    [Column("date")]
    public string Date {get;set;} = string.Empty;
    [ForeignKey(nameof(User))]
    [Column("user_id")]
    public int UserId {get;set;}
    public ICollection<Coordinate> Coordinates { get; set; } = new List<Coordinate>();
}