using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSHU.CarWash.EmailJob
{
    class DatabaseManager
    {

        static string ConnectionString = 
            "Data Source=tcp:mshucarwashdbserver.database.windows.net,1433;Initial Catalog=mshucarwash_db;User Id=hucarwas@mshucarwashdbserver;Password=Nimda/*!";


        public static string GetReservation(DateTime currentDay)
        {
            string connectionString = System.Environment.GetEnvironmentVariable("MSHUCarWashConnectionString");
            if (connectionString == null)
            {
                connectionString = ConnectionString;
            }
            
            string result = String.Empty;
            StringBuilder resultBuilder = new StringBuilder();
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                SqlCommand reservationQueryCommand = new SqlCommand();
                reservationQueryCommand.Connection = connection;
                reservationQueryCommand.CommandText =
                    "SELECT Name, Reservation.VehiclePlateNumber, Date,Reservation.EmployeeId, Comment " +
                    "FROM Reservation, Employee " +
                    "WHERE Reservation.EmployeeId = Employee.EmployeeId " +
                    "AND Reservation.Date = @Date";
                SqlParameter param = new SqlParameter();
                param.ParameterName = "@Date";
                param.Value = currentDay;
                reservationQueryCommand.Parameters.Add(param);

                // get data stream
                SqlDataReader reader = reservationQueryCommand.ExecuteReader();

                resultBuilder.AppendLine("<table>");
                resultBuilder.AppendLine("<tr><th>Név</th><th>Rendszám</th><th>Email</th><th>Dátum</th><th>Megjegyzés</th></tr>");
                // write each record
                while (reader.Read())
                {
                    resultBuilder.AppendLine("<tr>");
                    resultBuilder.AppendLine(String.Format("<td>{0}</td> <td>{1}</td> <td>{2}</td> <td>{3}</td> <td>{4}</td>",
                        reader["Name"],
                        reader["VehiclePlateNumber"],
                        reader["EmployeeId"],
                        reader["Date"], 
                        reader["Comment"]));
                    resultBuilder.AppendLine("</tr>");
                }
                resultBuilder.AppendLine("</table>");
                result = resultBuilder.ToString();
            }
            
            return result;
        }
    }
}
