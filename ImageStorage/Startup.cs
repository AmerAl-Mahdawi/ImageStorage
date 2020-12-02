using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using ImageStorage.Library.Config;
using ImageStorage.Library.Internal;
using ImageStorage.Library.Stores;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ImageStorage
{
    public class Startup
    {
        private readonly IConfiguration _config;

        public Startup(IConfiguration config)
        {
            _config = config;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public virtual void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            // Personal Services
            services.AddTransient<ISqlDataAccess, SqlDataAccess>();
            services.AddTransient<IImageStore, ImageStore>();

            IS3SvcConfiguration s3SvcConfig;

            CredentialProfile basicProfile;
            AWSCredentials awsCredentials;
            var sharedFile = new SharedCredentialsFile();

            if (sharedFile.TryGetProfile("default", out basicProfile) &&
         AWSCredentialsFactory.TryGetAWSCredentials(basicProfile, sharedFile, out awsCredentials))
            {
                s3SvcConfig = new S3SvcConfiguration()
                {
                    AccessKey = awsCredentials.GetCredentials().AccessKey,
                    SecretKey = awsCredentials.GetCredentials().SecretKey,
                    RegionPoint = basicProfile.Region,
                    BucketName = _config.GetSection("S3Service:BucketName").Value
                };
            }
            else
            {
                // Log an exception that aws credentials or s3 bucket name are not provided
                s3SvcConfig = new S3SvcConfiguration();
            }
            services.AddSingleton<IS3SvcConfiguration>(s3SvcConfig);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public virtual void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
