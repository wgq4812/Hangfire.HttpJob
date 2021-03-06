﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace Hangfire.HttpJob.Client
{
    internal class HttpJobItem
    {
        private readonly string _hangfireUrl;
        private readonly HangfireServerPostOption _httpPostOption;

        private HttpJobItem()
        {
            Method = "Post";
            ContentType = "application/json";
            Timeout = 20000;
            DelayFromMinutes = 15;
        }

        public HttpJobItem(string hangfireUrl, HangfireServerPostOption option) :this()
        {
            _hangfireUrl = hangfireUrl;
            _httpPostOption = option;
        }


        #region HttpJob
        /// <summary>
        /// 请求Url
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// 请求参数
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// 参数
        /// </summary>
        public string Data { get; set; }

        public string ContentType { get; set; }

        public int Timeout { get; set; }

        public int DelayFromMinutes { get; set; }
        public string Cron { get; set; }
        public string JobName { get; set; }
        public string QueueName { get; set; }

        /// <summary>
        /// 是否成功发送邮件
        /// </summary>
        public bool SendSucMail { get; set; }

        /// <summary>
        /// 是否失败发送邮件
        /// </summary>
        public bool SendFaiMail { get; set; }

        /// <summary>
        /// 指定发送邮件
        /// </summary>
        public string Mail { get; set; }

        /// <summary>
        /// 开启失败重启
        /// </summary>
        public bool EnableRetry { get; set; }

        /// <summary>
        /// 传了class就代表是agentjob
        /// </summary>
        public string AgentClass { get; set; }
        
        /// <summary>
        /// Header
        /// </summary>
        public Dictionary<string,string> Headers { get; set; } = new Dictionary<string, string>();

        public string BasicUserName { get; set; }
        public string BasicPassword { get; set; }
        #endregion

        /// <summary>
        /// 发送请求
        /// </summary>
        /// <returns></returns>
        public async Task<T> PostAsync<T>() where T: HangfirJobResult,new()
        {
            var result = new T();
            try
            {
                var client = _httpPostOption.HttpClient ?? HangfireJobClient.HangfireJobHttpClientFactory.GetHttpClient(_hangfireUrl);
                var httpMesage = PrepareHttpRequestMessage();
                if (_httpPostOption.TimeOut < 1) _httpPostOption.TimeOut = 5000;
                var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(_httpPostOption.TimeOut));
                var httpResponse = await client.SendAsync(httpMesage, cts.Token);

                if (httpResponse.StatusCode == HttpStatusCode.OK)
                {
                    if (result is AddBackgroundHangfirJobResult br)
                    {
                        br.JobId = await httpResponse.Content.ReadAsStringAsync();
                    }
                    result.IsSuccess = true;
                    return result;
                }

                if (httpResponse.StatusCode != HttpStatusCode.NoContent)
                {
                    result.IsSuccess = false;
                    result.ErrMessage = httpResponse.StatusCode.ToString();
                    return result;
                }
               
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrMessage = ex.Message;
                if (_httpPostOption.ThrowException) throw;
                return result;
            }

            result.IsSuccess = true;
            return result;
        }
        public T Post<T>() where T : HangfirJobResult, new()
        {
            return PostAsync<T>().ConfigureAwait(false).GetAwaiter().GetResult();
        }


        private  HttpRequestMessage PrepareHttpRequestMessage()
        {
            var request = new HttpRequestMessage(new HttpMethod("POST"), this._hangfireUrl);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var data = JsonConvert.SerializeObject(this);
            var bytes = Encoding.UTF8.GetBytes(data);
            request.Content = new ByteArrayContent(bytes, 0, bytes.Length);
            if (!string.IsNullOrEmpty(_httpPostOption.BasicUserName) && !string.IsNullOrEmpty(_httpPostOption.BasicPassword))
            {
                var byteArray = Encoding.ASCII.GetBytes(_httpPostOption.BasicUserName + ":" + _httpPostOption.BasicPassword);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            }
            return request;
        }

      
    }
}
