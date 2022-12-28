using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Amazon.S3;
using Amazon.Runtime;
using Amazon.S3.Model;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Security.Claims;
using System.Net.Http;
using System.Linq;
using Notesnook.API.Interfaces;
using System;

namespace Notesnook.API.Controllers
{
    [ApiController]
    [Route("s3")]
    [Authorize("Sync")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class S3Controller : ControllerBase
    {
        private IS3Service S3Service { get; set; }
        public S3Controller(IS3Service s3Service)
        {
            S3Service = s3Service;
        }

        [HttpPut]
        public IActionResult Upload([FromQuery] string name)
        {
            var userId = this.User.FindFirstValue("sub");
            var url = S3Service.GetUploadObjectUrl(userId, name);
            if (url == null) return BadRequest("Could not create signed url.");
            return Ok(url);
        }


        [HttpGet("multipart")]
        public async Task<IActionResult> MultipartUpload([FromQuery] string name, [FromQuery] int parts, [FromQuery] string uploadId)
        {
            var userId = this.User.FindFirstValue("sub");
            try
            {
                var meta = await S3Service.StartMultipartUploadAsync(userId, name, parts, uploadId);
                return Ok(meta);
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpDelete("multipart")]
        public async Task<IActionResult> AbortMultipartUpload([FromQuery] string name, [FromQuery] string uploadId)
        {
            var userId = this.User.FindFirstValue("sub");
            try
            {
                await S3Service.AbortMultipartUploadAsync(userId, name, uploadId);
                return Ok();
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpPost("multipart")]
        public async Task<IActionResult> CompleteMultipartUpload([FromBody] CompleteMultipartUploadRequest uploadRequest)
        {
            var userId = this.User.FindFirstValue("sub");
            try
            {
                await S3Service.CompleteMultipartUploadAsync(userId, uploadRequest);
                return Ok();
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpGet]
        [Authorize]
        public IActionResult Download([FromQuery] string name)
        {
            var userId = this.User.FindFirstValue("sub");
            var url = S3Service.GetDownloadObjectUrl(userId, name);
            if (url == null) return BadRequest("Could not create signed url.");
            return Ok(url);
        }

        [HttpHead]
        [Authorize]
        public async Task<IActionResult> Info([FromQuery] string name)
        {
            var userId = this.User.FindFirstValue("sub");
            var size = await S3Service.GetObjectSizeAsync(userId, name);
            if (size == null) return BadRequest();

            HttpContext.Response.Headers.ContentLength = size;
            return Ok();
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteAsync([FromQuery] string name)
        {
            try
            {
                var userId = this.User.FindFirstValue("sub");
                await S3Service.DeleteObjectAsync(userId, name);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
