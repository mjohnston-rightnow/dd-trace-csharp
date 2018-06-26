#pragma once

#include <string>
#include "json.hpp"

using json = nlohmann::json;

class Configuration
{
private:
    json m_json;

public:
    Configuration() = default;
    void Load(const std::wstring& fileName);
    std::wstring GetApplicationName() const;
    bool IsTracingEnabled() const;
    bool IsIntegrationEnabled(const std::string& integrationName) const;

    Configuration(const Configuration& other) = default;
    Configuration(Configuration&& other) noexcept = default;
    Configuration& operator=(const Configuration& other) = default;
    Configuration& operator=(Configuration&& other) noexcept = default;
};

extern Configuration GlobalConfiguration;
