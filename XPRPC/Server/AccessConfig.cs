/*
 * Copyright 2020, Guan Xiaopeng
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
 
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace XPRPC.Server
{
    public class AccessConfig
    {
        Dictionary<string, SortedSet<string>> _serviceGrantedClients = new Dictionary<string, SortedSet<string>>();

        SortedSet<string> _serviceProviders = new SortedSet<string>();

        Dictionary<string, string> _clientSecrets = new Dictionary<string, string>();

        public bool ClientCanAccessService(string clientID, string serviceName)
        {
            _serviceGrantedClients.TryGetValue(serviceName, out SortedSet<string> accessSet);
            return accessSet != null && (accessSet.Contains(clientID) || accessSet.Contains("any"));
        }

        public bool ClientIsServiceProvider(string clientID)
        {
            return _serviceProviders.Contains(clientID);
        }

        public bool ClientIsValid(string clientID, string secrectKey)
        {
            _clientSecrets.TryGetValue(clientID, out string secret);
            return !string.IsNullOrEmpty(secret) && secret == secrectKey;
        }

        public string ToJson()
        {
            using var stream = new MemoryStream();
            var options = new JsonWriterOptions{ Indented = true };
            using(var writer = new Utf8JsonWriter(stream, options))
            {
                writer.WriteStartObject();
                WriteServiceGrant(writer);
                WriteServiceProvider(writer);
                WriteClientSecret(writer);
                writer.WriteEndObject();
            }

            return Encoding.UTF8.GetString(stream.ToArray());
        }

        private void WriteServiceGrant(Utf8JsonWriter writer)
        {
            writer.WritePropertyName("service_client_access");
            writer.WriteStartObject();
            foreach(var item in _serviceGrantedClients)
            {
                writer.WritePropertyName(item.Key);
                writer.WriteStartArray();
                foreach(var c in item.Value)
                {
                    writer.WriteStringValue(c);
                }
                writer.WriteEndArray();
            }
            writer.WriteEndObject();
        }

        private void WriteServiceProvider(Utf8JsonWriter writer)
        {
            writer.WritePropertyName("service_provider_client");
            writer.WriteStartArray();
            foreach(var p in _serviceProviders)
            {
                writer.WriteStringValue(p);
            }
            writer.WriteEndArray();
        }

        private void WriteClientSecret(Utf8JsonWriter writer)
        {
            writer.WritePropertyName("client_secrect");
            writer.WriteStartObject();
            foreach(var item in _clientSecrets)
            {
                writer.WritePropertyName(item.Key);
                writer.WriteStringValue(item.Value);
            }
            writer.WriteEndObject();
        }

        public static AccessConfig FromJson(string json)
        {
            var result = new AccessConfig();
            var options = new JsonDocumentOptions {  AllowTrailingCommas = true  };
            using var document = JsonDocument.Parse(json, options);
            result.RestoreFromJsonDocument(document);

            return result;
        }

        private void RestoreFromJsonDocument(JsonDocument document)
        {
            RestoreClientSecret(document);
            RestoreServiceProvider(document);
            RestoreServiceGrant(document);
        }

        private void RestoreClientSecret(JsonDocument document)
        {
            var ele = document.RootElement.GetProperty("client_secrect");
            foreach(var iter in ele.EnumerateObject())
            {
                var clientID = iter.Name;
                var secret = iter.Value.GetString();
                _clientSecrets[clientID] = secret;
            }
        }

        private void RestoreServiceProvider(JsonDocument document)
        {
            var ele = document.RootElement.GetProperty("service_provider_client");
            foreach(var iter in ele.EnumerateArray())
            {
                var clientID = iter.GetString();
                _serviceProviders.Add(clientID);
            }
        }

        private void RestoreServiceGrant(JsonDocument document)
        {
            var ele = document.RootElement.GetProperty("service_client_access");
            foreach(var iter in ele.EnumerateObject())
            {
                var serviceName = iter.Name;
                var grantClients = new SortedSet<string>();
                _serviceGrantedClients[serviceName] = grantClients;
                foreach(var item in iter.Value.EnumerateArray())
                {
                    grantClients.Add(item.GetString());
                }
            }
        }
    }
}
