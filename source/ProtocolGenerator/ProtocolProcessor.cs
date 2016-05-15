﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterDevs.ChromeDevTools.ProtocolGenerator
{
    public class ProtocolProcessor
    {
        public static void ResolveTypeReferences(Protocol protocol)
        {
            foreach (var domain in protocol.Domains)
            {
                ResolveTypeReferences(protocol, domain);
            }
        }

        public static void ResolveTypeReferences(Protocol protocol, Domain domain)
        {
            foreach (var command in domain.Commands)
            {
                ResolveTypeReferences(protocol, domain, command);
            }
        }

        public static void ResolveTypeReferences(Protocol protocol, Domain domain, Command command)
        {
            foreach (var parameter in command.Parameters)
            {
                ResolveTypeReferences(protocol, domain, parameter);
            }

            foreach (var returnValue in command.Returns)
            {
                ResolveTypeReferences(protocol, domain, returnValue);
            }
        }

        public static void ResolveTypeReferences(Protocol protocol, Domain domain, Property property)
        {
            if (property.TypeReference != null)
            {
                // Find the type which is being referenced
                var referenceParts = property.TypeReference.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

                Domain referencedDomain = null;
                Type referencedType = null;

                if (referenceParts.Length == 1)
                {
                    referencedDomain = domain;
                    referencedType = domain.GetType(referenceParts[0]);
                }
                else if (referenceParts.Length == 2)
                {
                    referencedDomain = protocol.GetDomain(referenceParts[0]);
                    referencedType = referencedDomain.GetType(referenceParts[1]);
                }
                else
                {
                    throw new ArgumentOutOfRangeException();
                }

                // If it is a string, it can be resolved easily
                if(referencedType.IsString())
                {
                    property.Kind = "string";
                    property.TypeReference = null;
                }
            }
        }

        public static Protocol LoadProtocol(string path, string alias)
        {
            string json = File.ReadAllText(path);
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.MissingMemberHandling = MissingMemberHandling.Error;
            settings.MetadataPropertyHandling = MetadataPropertyHandling.Ignore;
            Protocol p = JsonConvert.DeserializeObject<Protocol>(json, settings);
            p.SourceFile = path;
            p.Alias = alias;

            foreach (var domain in p.Domains)
            {
                foreach (var command in domain.Commands)
                {
                    command.SupportedBy.Add(alias);
                }

                foreach (var @event in domain.Events)
                {
                    @event.SupportedBy.Add(alias);
                }

                foreach (var type in domain.Types)
                {
                    type.SupportedBy.Add(alias);
                }
            }

            return p;
        }
    }
}