#include "Configuration.h"
#include <fstream>

void Configuration::Load(const std::wstring& fileName)
{
    std::ifstream file(fileName);
    file >> m_json;
}

std::wstring Configuration::GetApplicationName() const
{
    return m_json["tracing"]["applicationName"];
}

bool Configuration::IsTracingEnabled() const
{
    return m_json["tracing"]["enabled"];
}

bool Configuration::IsIntegrationEnabled(const std::string& integrationName) const
{
    const json integrations = m_json["tracing"]["integrations"];
    const json::const_iterator integrationConfiguration = integrations.find(integrationName);

    if(integrationConfiguration == integrations.end())
    {
        // integration configuration not found
        return false;
    }

    return integrationConfiguration.value()["enabled"];
}

Configuration GlobalConfiguration;