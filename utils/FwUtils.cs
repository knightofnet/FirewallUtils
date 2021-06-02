using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AryxDevLibrary.extensions;
using AryxDevLibrary.utils;
using AryxDevLibrary.utils.logger;
using NetFwTypeLib;
using PocFwIpApp.constant;

namespace PocFwIpApp.utils
{
    public static class FwUtils
    {
        private static Logger log = Logger.LastLoggerInstance;

        public static INetFwRule CreateRuleForRemoteAddresses(string name, DirectionsEnum direction, ProtocoleEnum protocole, bool isAllow, String description, List<string> adress)
        {
            INetFwRule firewallRule = (INetFwRule)Activator.CreateInstance(
                Type.GetTypeFromProgID("HNetCfg.FWRule"));

            INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(
                Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));

            firewallRule.Protocol = (int)protocole;
            firewallRule.RemoteAddresses = String.Join(",", adress);

            firewallRule.Action = isAllow ? NET_FW_ACTION_.NET_FW_ACTION_ALLOW : NET_FW_ACTION_.NET_FW_ACTION_BLOCK;
            firewallRule.Description = description;
            firewallRule.Enabled = true;
            firewallRule.InterfaceTypes = "All";
            firewallRule.Name = name;
            firewallRule.Direction = DirectionEnumToFwRuleDirection(direction);

            firewallPolicy.Rules.Add(firewallRule);

            return firewallRule;
        }



        public static INetFwRule CreateRuleForProgram(string ruleName, DirectionsEnum direction, ProtocoleEnum protocole, bool isAllow, string description, string programFilePath)
        {
            INetFwRule firewallRule = (INetFwRule)Activator.CreateInstance(
                Type.GetTypeFromProgID("HNetCfg.FWRule"));

            INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(
                Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));

            firewallRule.Protocol = (int)protocole;
            firewallRule.ApplicationName = programFilePath;
            firewallRule.Action = isAllow ? NET_FW_ACTION_.NET_FW_ACTION_ALLOW : NET_FW_ACTION_.NET_FW_ACTION_BLOCK;
            firewallRule.Description = description;
            firewallRule.Enabled = true;
            firewallRule.InterfaceTypes = "All";
            firewallRule.Name = ruleName;
            firewallRule.Direction = DirectionEnumToFwRuleDirection(direction);

            firewallPolicy.Rules.Add(firewallRule);

            return firewallRule;
        }

        public static void UpdateRuleForRemoteAddresses(string getName, DirectionsEnum getDirection,
            ProtocoleEnum getProtocole, List<string> adress)
        {
            INetFwRule firewallRule = (INetFwRule)Activator.CreateInstance(
                Type.GetTypeFromProgID("HNetCfg.FWRule"));

            INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(
                Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));

            firewallRule = firewallPolicy.Rules.Item(getName);
            if (firewallRule.Direction == DirectionEnumToFwRuleDirection(getDirection) &&
                firewallRule.Protocol == (int)getProtocole)
            {
                firewallRule.RemoteAddresses = String.Join(",", adress);
            }
        }

        public static INetFwRule GetRule(string getName, DirectionsEnum getDirection, ProtocoleEnum getProtocole)
        {
            try
            {
                String uniqueS = StringUtils.RandomString(8, ensureUnique: true);

                INetFwRule firewallRule = (INetFwRule)Activator.CreateInstance(
                    Type.GetTypeFromProgID("HNetCfg.FWRule"));

                INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(
                    Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));


                foreach (INetFwRule fwRule in firewallPolicy.Rules)
                {

                    if (fwRule.Name != null)
                    {

                        if (fwRule.Name.Equals(getName) && fwRule.Protocol == (int)getProtocole)
                        {
                         //   log.Debug("GetRule n:{0} p:{1} OK ({2})", getName, getProtocole, uniqueS);
                            if (getDirection == DirectionsEnum.NULL ||
                                getDirection == FwRuleDirectionToDirectionEnum(fwRule.Direction))
                            {
                               //log.Debug("GetRule n:{0} d:{1} OK ({2})", fwRule.Name, fwRule.Direction, fwRule.RemoteAddresses);
                                return fwRule;
                            }
                        }

                    }
                }


                //log.Debug("GetRule {0} {1} {2} => null" + uniqueS, getName, getDirection, getProtocole);
                return null;
            }
            catch (Exception e)
            {
                ExceptionHandlingUtils.LogAndHideException(e, "GetRule"); 
                return null;
            }
        }

        public static List<INetFwRule> GetRuleNameRegex(string regex, DirectionsEnum getDirection, ProtocoleEnum getProtocole)
        {
            List<INetFwRule> retListRules = new List<INetFwRule>();

            try
            {
              
                INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(
                    Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));

                foreach (INetFwRule fwRule in firewallPolicy.Rules)
                {

                    if (getDirection == DirectionsEnum.NULL || getDirection == FwRuleDirectionToDirectionEnum(fwRule.Direction))
                    {
                        if (fwRule.Name != null && fwRule.Name.Matches(regex))
                        {
                            retListRules.Add(fwRule);
                        }
                    }
                }

                return retListRules;
            }
            catch (Exception e)
            {
                ExceptionHandlingUtils.LogAndHideException(e, "GetRuleNameRegex");
                throw e;
            }
        }

        public static bool IsExistsRule(string getName, DirectionsEnum getDirection, ProtocoleEnum getProtocole)
        {
            try
            {
                String uniqueS = StringUtils.RandomString(8, ensureUnique: true);

                INetFwRule firewallRule = (INetFwRule)Activator.CreateInstance(
                    Type.GetTypeFromProgID("HNetCfg.FWRule"));

                INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(
                    Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));

                foreach (INetFwRule fwRule in firewallPolicy.Rules)
                {
                    
                    if (fwRule.Name != null)
                    {
                       
                        if (fwRule.Name.Equals(getName) && fwRule.Protocol == (int)getProtocole)
                        {
                            //log.Debug("IsExistRule n:{0} p:{1} OK ({2})", getName, getProtocole, uniqueS);
                            if (getDirection == DirectionsEnum.NULL ||
                                getDirection == FwRuleDirectionToDirectionEnum(fwRule.Direction))
                            {
                               // log.Debug("IsExistRule n:{0} d:{1} OK ({2})", getName, getDirection, uniqueS);
                                return true;
                            }
                        }
                      
                    }
                }


                log.Debug("IsExistRule {0} {1} {2} "+ uniqueS, getName, getDirection, getProtocole);
                return false;
                /*
                firewallRule = firewallPolicy.Rules.Item(getName);
                if (firewallRule.Direction == DirectionEnumToFwRuleDirection(getDirection) &&
                    firewallRule.Protocol == (int)getProtocole)
                {
                    return true;
                }

                return false;
                */
            }
            catch (Exception e)
            {
                ExceptionHandlingUtils.LogAndHideException(e, "IsExistsRule");
                return false;
            }
        }

        public static bool IsExistsRuleForProgram(string programFilepath, DirectionsEnum getDirection = DirectionsEnum.NULL)
        {
            try
            {

                INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(
                    Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));

                foreach (INetFwRule fwRule in firewallPolicy.Rules)
                {
                    if (fwRule.ApplicationName != null && fwRule.ApplicationName.Equals(programFilepath, StringComparison.CurrentCultureIgnoreCase))
                    {

                        if (getDirection == DirectionsEnum.NULL ||
                            getDirection == FwRuleDirectionToDirectionEnum(fwRule.Direction))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
            catch (Exception e)
            {
                ExceptionHandlingUtils.LogAndHideException(e, "IsExistsRuleForProgram");

                return false;
            }
        }

        public static INetFwRule GetRuleForProgram(string programFilepath, DirectionsEnum getDirection = DirectionsEnum.NULL)
        {
            try
            {

                INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(
                    Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));

                foreach (INetFwRule fwRule in firewallPolicy.Rules)
                {
                    if (fwRule.ApplicationName != null && fwRule.ApplicationName.Equals(programFilepath, StringComparison.CurrentCultureIgnoreCase))
                    {

                        if (getDirection == DirectionsEnum.NULL ||
                            getDirection == FwRuleDirectionToDirectionEnum(fwRule.Direction))
                        {
                            return fwRule;
                        }
                    }
                }

                return null;
            }
            catch (Exception e)
            {
                ExceptionHandlingUtils.LogAndHideException(e, "GetRuleForProgram");

                return null;
            }

        }

        internal static IEnumerable<INetFwRule> GetRulesWithProgram(DirectionsEnum getDirection = DirectionsEnum.NULL)
        {
            List<INetFwRule> retList = new List<INetFwRule>();
            try
            {

                INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(
                    Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));

                foreach (INetFwRule fwRule in firewallPolicy.Rules)
                {
                    if (!String.IsNullOrWhiteSpace(fwRule.ApplicationName))
                    {

                        if (getDirection == DirectionsEnum.NULL ||
                            getDirection == FwRuleDirectionToDirectionEnum(fwRule.Direction))
                        {
                            retList.Add(fwRule);
                        }
                    }
                }

            }
            catch (Exception e)
            {
                ExceptionHandlingUtils.LogAndHideException(e, "GetRulesWithProgram");

            }

            return retList.ToArray();
        }

        internal static INetFwRule[] GetRulesForProgram(string programFilepath, DirectionsEnum getDirection = DirectionsEnum.NULL)
        {
            List<INetFwRule> retList = new List<INetFwRule>();
            try
            {

                INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(
                    Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));

                foreach (INetFwRule fwRule in firewallPolicy.Rules)
                {
                    if (fwRule.ApplicationName != null && fwRule.ApplicationName.Equals(programFilepath, StringComparison.CurrentCultureIgnoreCase))
                    {

                        if (getDirection == DirectionsEnum.NULL ||
                            getDirection == FwRuleDirectionToDirectionEnum(fwRule.Direction))
                        {
                            retList.Add(fwRule);
                        }
                    }
                }

            }
            catch (Exception e)
            {
                ExceptionHandlingUtils.LogAndHideException(e, "GetRuleForProgram");

            }

            return retList.ToArray();
        }

        public static NET_FW_RULE_DIRECTION_ DirectionEnumToFwRuleDirection(DirectionsEnum dEnum)
        {
            switch (dEnum)
            {
                case DirectionsEnum.Outbound:
                    return NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT;
                case DirectionsEnum.Inboud:
                    return NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN;
                case DirectionsEnum.Both:
                    return NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_MAX;
                default:
                    throw new Exception("Pas de direction associée");
            }
        }

        public static DirectionsEnum FwRuleDirectionToDirectionEnum(NET_FW_RULE_DIRECTION_ direction)
        {
            switch (direction)
            {
                case NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT:
                    return DirectionsEnum.Outbound;
                case NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN:
                    return DirectionsEnum.Inboud;
                case NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_MAX:
                    return DirectionsEnum.Both;
                default:
                    throw new Exception("Pas de direction associée");
            }
        }


    }
}
