using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicStore.Api.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class BackupController : ControllerBase
    {
        private readonly IDockerClient _dockerClient;

        public BackupController()
        {
            var config = new DockerClientConfiguration(new Uri("unix:///var/run/docker.sock"));
            _dockerClient = config.CreateClient();
        }
        [HttpPost("start")]
        public async Task<IActionResult> StartBackup()
        {
            const string containerName = "sqlserver_backup_runner";

            try
            {
                var started = await _dockerClient.Containers.StartContainerAsync(
                    containerName,
                    new ContainerStartParameters()
                );

                if (started)
                {                    
                    return Ok($"Backup container '{containerName}' started successfully.");
                }
                else
                {
                    return StatusCode(500, $"Failed to start container '{containerName}'.");
                }
            }
            catch (DockerApiException ex)
            {
                return StatusCode(500, $"Docker API Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
        }
    }
}
