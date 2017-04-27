using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace EFSqlTranslator.Tests
{
    public class TestingContext : DbContext
    {
        public const string ConnectionString = ":memory:";

        public DbSet<Blog> Blogs { get; set; }
        
        public DbSet<Post> Posts { get; set; }

        public DbSet<User> Users { get; set; }

        public DbSet<Comment> Comments { get; set; }

        public DbSet<Statistic> Statistics { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(ConnectionString);
        }
    }

    public class Blog
    {
        public int BlogId { get; set; }
        
        public string Url { get; set; }
        
        public string Name { get; set; }
        
        public int UserId { get; set; }

        public int? CommentCount { get; set; }
        
        public User User { get; set; }

        public Statistic Statistic { get; set; }
        
        public List<Post> Posts { get; set; }

        public List<Comment> Comments { get; set; }
    }

    public class Post
    {
        public int PostId { get; set; }
        
        public string Title { get; set; }
        
        public string Content { get; set; }

        public int LikeCount { get; set; }
        
        public int BlogId { get; set; }

        public int UserId { get; set; }
        
        public User User { get; set; }

        public Blog Blog { get; set; }

        public List<Comment> Comments { get; set; }
    }

    public class User
    {
        public int UserId { get; set; }

        public string UserName { get; set; }

        public List<Blog> Blogs { get; set; }

        public List<Post> Posts { get; set; }

        public List<Comment> Comments { get; set; }
    }

    public class Comment
    {
        public int CommentId { get; set; }

        public int UserId { get; set; }

        public int BlogId { get; set; }

        public int PostId { get; set; }

        public User User { get; set; }

        public Blog Blog { get; set; }

        public Post Post { get; set; }
    }

    public class Statistic
    {
        public int StatisticId { get; set; }

        public int ViewCount { get; set; }

        public float FloatVal { get; set; }

        public decimal DecimalVal { get; set; }

        public double DoubleVal { get; set; }

        public int BlogId { get; set; }

        public Blog Blog { get; set; }
    }
}