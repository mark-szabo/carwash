namespace MSHU.CarWash.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Initial : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Employee",
                c => new
                    {
                        EmployeeId = c.String(nullable: false, maxLength: 128),
                        Name = c.String(nullable: false, maxLength: 100),
                        VehiclePlateNumber = c.String(maxLength: 7),
                        Email = c.String(nullable: false, maxLength: 100),
                        CreatedBy = c.String(nullable: false, maxLength: 100),
                        CreatedOn = c.DateTime(nullable: false),
                        ModifiedBy = c.String(nullable: false, maxLength: 100),
                        ModifiedOn = c.DateTime(nullable: false),
                        TimeStamp = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    })
                .PrimaryKey(t => t.EmployeeId);
            
            CreateTable(
                "dbo.Reservation",
                c => new
                    {
                        ReservationId = c.Int(nullable: false, identity: true),
                        EmployeeId = c.String(maxLength: 128),
                        SelectedServiceId = c.Int(nullable: false),
                        VehiclePlateNumber = c.String(nullable: false, maxLength: 7),
                        Date = c.DateTime(nullable: false),
                        Comment = c.String(maxLength: 250),
                        CreatedBy = c.String(nullable: false, maxLength: 100),
                        CreatedOn = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.ReservationId)
                .ForeignKey("dbo.Employee", t => t.EmployeeId)
                .Index(t => t.EmployeeId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Reservation", "EmployeeId", "dbo.Employee");
            DropIndex("dbo.Reservation", new[] { "EmployeeId" });
            DropTable("dbo.Reservation");
            DropTable("dbo.Employee");
        }
    }
}
