# Copyright (c) Microsoft. All rights reserved.


from logging import Logger
from typing import Dict, Optional, overload

from openai.lib.azure import AsyncAzureADTokenProvider

from semantic_kernel.connectors.ai.open_ai.const import DEFAULT_AZURE_API_VERSION
from semantic_kernel.connectors.ai.open_ai.services.azure_config_base import (
    AzureOpenAIConfigBase,
)
from semantic_kernel.connectors.ai.open_ai.services.open_ai_handler import (
    OpenAIModelTypes,
)
from semantic_kernel.connectors.ai.open_ai.services.open_ai_text_completion_base import (
    OpenAITextCompletionBase,
)


class AzureTextCompletion(AzureOpenAIConfigBase, OpenAITextCompletionBase):
    """Azure Text Completion class."""

    @overload
    def __init__(
        self,
        base_url: str,
        api_version: str = DEFAULT_AZURE_API_VERSION,
        api_key: Optional[str] = None,
        ad_token: Optional[str] = None,
        ad_token_provider: Optional[AsyncAzureADTokenProvider] = None,
        log: Optional[Logger] = None,
    ) -> None:
        """
        Initialize an AzureTextCompletion service.

        Arguments:
            deployment_name: The name of the Azure deployment. This value
                will correspond to the custom name you chose for your deployment
                when you deployed a model. This value can be found under
                Resource Management > Deployments in the Azure portal or, alternatively,
                under Management > Deployments in Azure OpenAI Studio.
            endpoint: The endpoint of the Azure deployment. This value
                can be found in the Keys & Endpoint section when examining
                your resource from the Azure portal.
            api_key: The API key for the Azure deployment. This value can be
                found in the Keys & Endpoint section when examining your resource in
                the Azure portal. You can use either KEY1 or KEY2.
            api_version: The API version to use. (Optional)
                The default value is "2023-05-15".
            ad_auth: Whether to use Azure Active Directory authentication. (Optional)
                The default value is False.
            log: The logger instance to use. (Optional)
            logger: deprecated, use 'log' instead.
        """

    @overload
    def __init__(
        self,
        deployment_name: str,
        endpoint: str,
        api_version: str = DEFAULT_AZURE_API_VERSION,
        api_key: Optional[str] = None,
        ad_token: Optional[str] = None,
        ad_token_provider: Optional[AsyncAzureADTokenProvider] = None,
        log: Optional[Logger] = None,
    ) -> None:
        """
        Initialize an AzureTextCompletion service.

        Arguments:
            deployment_name: The name of the Azure deployment. This value
                will correspond to the custom name you chose for your deployment
                when you deployed a model. This value can be found under
                Resource Management > Deployments in the Azure portal or, alternatively,
                under Management > Deployments in Azure OpenAI Studio.
            endpoint: The endpoint of the Azure deployment. This value
                can be found in the Keys & Endpoint section when examining
                your resource from the Azure portal.
            api_key: The API key for the Azure deployment. This value can be
                found in the Keys & Endpoint section when examining your resource in
                the Azure portal. You can use either KEY1 or KEY2.
            api_version: The API version to use. (Optional)
                The default value is "2023-05-15".
            ad_auth: Whether to use Azure Active Directory authentication. (Optional)
                The default value is False.
            log: The logger instance to use. (Optional)
            logger: deprecated, use 'log' instead.
        """

    def __init__(
        self,
        deployment_name: Optional[str] = None,
        endpoint: Optional[str] = None,
        base_url: Optional[str] = None,
        api_version: str = DEFAULT_AZURE_API_VERSION,
        api_key: Optional[str] = None,
        ad_token: Optional[str] = None,
        ad_token_provider: Optional[AsyncAzureADTokenProvider] = None,
        log: Optional[Logger] = None,
        logger: Optional[Logger] = None,
    ) -> None:
        """
        Initialize an AzureTextCompletion service.

        Arguments:
            deployment_name: The name of the Azure deployment. This value
                will correspond to the custom name you chose for your deployment
                when you deployed a model. This value can be found under
                Resource Management > Deployments in the Azure portal or, alternatively,
                under Management > Deployments in Azure OpenAI Studio.
            endpoint: The endpoint of the Azure deployment. This value
                can be found in the Keys & Endpoint section when examining
                your resource from the Azure portal.
            api_key: The API key for the Azure deployment. This value can be
                found in the Keys & Endpoint section when examining your resource in
                the Azure portal. You can use either KEY1 or KEY2.
            api_version: The API version to use. (Optional)
                The default value is "2023-03-15-preview".
            ad_auth: Whether to use Azure Active Directory authentication. (Optional)
                The default value is False.
            log: The logger instance to use. (Optional)
            logger: deprecated, use 'log' instead.
        """
        if logger:
            logger.warning("The 'logger' argument is deprecated, use 'log' instead.")
        super().__init__(
            deployment_name=deployment_name,
            endpoint=endpoint,
            base_url=base_url,
            api_version=api_version,
            api_key=api_key,
            ad_token=ad_token,
            ad_token_provider=ad_token_provider,
            log=log or logger,
            ai_model_type=OpenAIModelTypes.TEXT,
        )

    @classmethod
    def from_dict(cls, settings: Dict[str, str]) -> "AzureTextCompletion":
        """
        Initialize an Azure OpenAI service from a dictionary of settings.

        Arguments:
            settings: A dictionary of settings for the service.
                should contains keys: deployment_name, endpoint, api_key
                and optionally: api_version, ad_auth, log
        """

        return AzureTextCompletion(
            deployment_name=settings.get("deployment_name"),
            endpoint=settings.get("endpoint"),
            base_url=settings.get("base_url"),
            api_version=settings.get("api_version", DEFAULT_AZURE_API_VERSION),
            api_key=settings["api_key"],
            ad_token=settings.get("ad_token"),
            ad_token_provider=settings.get("ad_token_provider"),
            log=settings.get("log"),
        )
