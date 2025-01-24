using Microsoft.AspNetCore.Mvc;
using PirMovementBlazorServer.Connection;
using PirMovementBlazorServer.Models;
using Dapper;
using System.Data;
using PirMovementBlazorServer.Services;

namespace PirMovementBlazorServer.Controllers
{
    // Movement Controller. Post and Get data from db

    [Route("api/[controller]")]
    [ApiController]
    public class MovementsController : ControllerBase
    {
        private readonly DbConnectionFactory _connectionFactory;
        private MovementListService _movementListService;
        public MovementsController(DbConnectionFactory connectionFactory, MovementListService movementListService)
        {
            _connectionFactory = connectionFactory;
            _movementListService = movementListService;
        }

        private const string sqlGet = "SELECT * FROM pir ORDER BY MovementTime DESC LIMIT 8";

        private const string sqlPost = "INSERT INTO pir(MovementTime) VALUES (@Start)";

        // GET: api/<ValuesController>
        // Get last 8 movements
        [HttpGet]
        public async Task<IEnumerable<Movement>> Get(CancellationToken cancellationToken)
        {
            using (var connection = _connectionFactory.CreateConnection())
            {
                await connection.OpenAsync(cancellationToken);
                await using (var transaction = await connection.BeginTransactionAsync(cancellationToken))
                {
                    try
                    {
                        var result = await connection.QueryAsync<Movement>(sqlGet, transaction: transaction);
                        await transaction.CommitAsync(cancellationToken);
                        return result;
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        throw;
                    }
                }
            }
        }

        // POST api/<ValuesController>
        [HttpPost]
        public async Task Post([FromBody] MovementValue movementV)
        {
            Movement move = new Movement { MovementTime = DateTime.Now };
            _movementListService.AddMovement(move);

            var dynamicParameters = new DynamicParameters();
            dynamicParameters.Add("@Start", DateTime.Now, DbType.DateTime);

            // Send data to database
            using (var connection = _connectionFactory.CreateConnection())
            {
                await connection.OpenAsync();
                using (var transaktion = await connection.BeginTransactionAsync())
                {
                    await connection.ExecuteAsync(sqlPost, dynamicParameters, transaktion);
                    transaktion.Commit();
                }
            }
        }
    }
}
