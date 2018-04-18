// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Script.Config;
using Microsoft.Azure.WebJobs.Script.WebHost.Management;
using Microsoft.Azure.WebJobs.Script.WebHost.Models;
using Microsoft.Azure.WebJobs.Script.WebHost.Security.Authorization.Policies;

namespace Microsoft.Azure.WebJobs.Script.WebHost.Controllers
{
    public class InstanceController : Controller
    {
        private readonly WebScriptHostManager _scriptHostManager;
        private readonly ScriptSettingsManager _settingsManager;
        private readonly IInstanceManager _instanceManager;

        public InstanceController(WebScriptHostManager scriptHostManager, ScriptSettingsManager settingsManager, IInstanceManager instanceManager)
        {
            _scriptHostManager = scriptHostManager;
            _settingsManager = settingsManager;
            _instanceManager = instanceManager;
        }

        [HttpPost]
        [Route("admin/instance/assign")]
        //[Authorize(Policy = PolicyNames.AdminAuthLevelOrInternal)]
        public IActionResult Assign([FromBody] EncryptedAssignmentContext encryptedAssignmentContext)
        {
            var containerKey = _settingsManager.GetSetting(ScriptConstants.ContainerEncryptionKey);
            var assignmentContext = encryptedAssignmentContext.Decrypt(containerKey);
            return _instanceManager.TryAssign(assignmentContext)
                ? Accepted()
                : StatusCode(StatusCodes.Status409Conflict, "Instance already assigned");
        }

        [HttpGet]
        [Route("admin/instance/status")]
        //[Authorize(Policy = PolicyNames.AdminAuthLevelOrInternal)]
        public async Task<IActionResult> GetInstanceStatus([FromQuery] int timeout = int.MaxValue)
        {
            return await _scriptHostManager.DelayUntilHostReady(timeoutSeconds: timeout)
                ? Ok()
                : StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        [HttpGet]
        [Route("admin/instance/info")]
        //[Authorize(Policy = PolicyNames.AdminAuthLevelOrInternal)]
        public IActionResult GetInstanceInfo()
        {
            return Ok(_instanceManager.GetInstanceInfo());
        }

        [HttpGet]
        [Route("admin/host/env")]
        public IActionResult GetEnv()
        {
            var dic = new Dictionary<string, string>();
            var dic2 = new Dictionary<string, string>();
            foreach (DictionaryEntry de in System.Environment.GetEnvironmentVariables())
            {
                dic.Add(de.Key.ToString(), de.Value.ToString());
            }

            foreach (var pair in dic.Select(p => new { key = p.Key, value = ScriptSettingsManager.Instance.GetSetting(p.Key) }))
            {
                dic2.Add(pair.key, pair.value);
            }

            return Ok(new
            {
                env = dic,
                setting = dic2
            });
        }
    }
}
