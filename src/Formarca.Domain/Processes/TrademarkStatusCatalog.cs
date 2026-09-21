namespace Formarca.Domain.Processes;

public static class TrademarkStatusCatalog
{
    public sealed record Definition(
        string Code,
        string Title,
        string Description,
        int SortOrder,
        TrademarkStatusPhase Phase,
        TrademarkStatusTone Tone);

    public static IReadOnlyList<Definition> All { get; } =
    [
        Item(
            "deposito-efetuado",
            "Depósito efetuado / Pedido protocolado",
            "O pedido foi recebido e aguarda a triagem inicial.",
            TrademarkStatusPhase.Filing,
            TrademarkStatusTone.Progress),
        Item(
            "aguardando-exame-formal",
            "Aguardando exame formal",
            "O INPI está verificando a documentação, enquadramento e comprovantes de pagamento.",
            TrademarkStatusPhase.Filing,
            TrademarkStatusTone.Progress),
        Item(
            "exigencia-formal",
            "Exigência formal",
            "Foram identificadas pendências documentais ou de taxa. Há um prazo de 5 dias para cumprimento.",
            TrademarkStatusPhase.Filing,
            TrademarkStatusTone.Attention),
        Item(
            "pedido-inexistente",
            "Pedido considerado inexistente",
            "Ocorreu quando uma exigência formal não foi cumprida no prazo ou o pagamento não foi identificado.",
            TrademarkStatusPhase.Filing,
            TrademarkStatusTone.Danger),
        Item(
            "publicado-oposicao",
            "Publicado pedido de registro de marca para oposição",
            "O pedido superou o exame formal e foi publicado na RPI (Revista da Propriedade Industrial) para conhecimento público.",
            TrademarkStatusPhase.Opposition,
            TrademarkStatusTone.Progress),
        Item(
            "aguardando-manifestacao-oposicao",
            "Aguardando manifestação sobre oposição",
            "Uma empresa ou terceiro protocolou uma oposição ao seu registro. Abre-se o prazo de 60 dias para defesa (contrarrazões).",
            TrademarkStatusPhase.Opposition,
            TrademarkStatusTone.Attention),
        Item(
            "notificacao-oposicao",
            "Notificação de oposição",
            "O INPI notifica o titular sobre a existência de uma oposição interposta.",
            TrademarkStatusPhase.Opposition,
            TrademarkStatusTone.Attention),
        Item(
            "manifestacao-oposicao-apresentada",
            "Manifestação à oposição apresentada",
            "A defesa contra a oposição foi protocolada com sucesso.",
            TrademarkStatusPhase.Opposition,
            TrademarkStatusTone.Progress),
        Item(
            "aguardando-exame-merito",
            "Aguardando exame de mérito",
            "O pedido concluiu a fase de oposição e está na fila para análise técnica dos examinadores.",
            TrademarkStatusPhase.Merit,
            TrademarkStatusTone.Progress),
        Item(
            "exigencia-merito",
            "Exigência de mérito",
            "O examinador solicita documentos adicionais, adequação da especificação de produtos/serviços ou esclarecimentos. Prazo de 60 dias para resposta.",
            TrademarkStatusPhase.Merit,
            TrademarkStatusTone.Attention),
        Item(
            "sobrestamento",
            "Sobrestando o exame de mérito (Sobrestamento)",
            "A análise do pedido foi congelada/pausada aguardando a decisão final de outro processo (ex.: uma marca idêntica/similar anterior que está em fase de recurso ou caducidade).",
            TrademarkStatusPhase.Merit,
            TrademarkStatusTone.Paused),
        Item(
            "deferimento",
            "Deferimento do pedido de registro",
            "O pedido foi aprovado. Abre-se o prazo de 60 dias para pagamento das taxas federais do primeiro decênio e emissão do certificado.",
            TrademarkStatusPhase.Decision,
            TrademarkStatusTone.Success),
        Item(
            "indeferimento",
            "Indeferimento do pedido de registro",
            "O pedido foi negado pelo INPI. Cabe recurso no prazo de 60 dias.",
            TrademarkStatusPhase.Decision,
            TrademarkStatusTone.Danger),
        Item(
            "recurso-interposto",
            "Recurso interposto / Publicado recurso",
            "O recurso contra o indeferimento foi protocolado e aguarda análise da segunda instância do INPI.",
            TrademarkStatusPhase.Appeal,
            TrademarkStatusTone.Progress),
        Item(
            "notificacao-contrarrazoes",
            "Notificação de contrarrazões ao recurso",
            "Caso um terceiro tenha feito o recurso (ex.: após deferimento contestado), você é notificado para responder.",
            TrademarkStatusPhase.Appeal,
            TrademarkStatusTone.Attention),
        Item(
            "recurso-provido",
            "Recurso provido",
            "A decisão de indeferimento foi revertida; o pedido passa a ser deferido.",
            TrademarkStatusPhase.Appeal,
            TrademarkStatusTone.Success),
        Item(
            "recurso-nao-provido",
            "Recurso não provido (Mantido o indeferimento)",
            "A negação da marca foi mantida de forma definitiva na esfera administrativa.",
            TrademarkStatusPhase.Appeal,
            TrademarkStatusTone.Danger),
        Item(
            "concessao-registro",
            "Concessão de registro",
            "As taxas finais foram pagas e o INPI concedeu os direitos exclusivos sobre a marca (válidos por 10 anos).",
            TrademarkStatusPhase.Grant,
            TrademarkStatusTone.Success),
        Item(
            "certificado-expedido",
            "Certificado de registro expedido",
            "O documento oficial comprovando a propriedade da marca está disponível para download.",
            TrademarkStatusPhase.Grant,
            TrademarkStatusTone.Success),
        Item(
            "pedido-arquivado",
            "Pedido arquivado definitivamente",
            "Ocorreu a aprovação (deferimento), mas o titular não pagou as taxas relativas ao primeiro decênio no prazo legal.",
            TrademarkStatusPhase.Grant,
            TrademarkStatusTone.Danger),
        Item(
            "aguardando-prorrogacao",
            "Aguardando pedido de prorrogação (Renovação)",
            "Aproxima-se o fim dos 10 anos e aguarda-se a renovação do registro.",
            TrademarkStatusPhase.Renewal,
            TrademarkStatusTone.Attention),
        Item(
            "registro-prorrogado",
            "Registro prorrogado",
            "O titular pagou a taxa do novo decênio, renovando a proteção por mais 10 anos.",
            TrademarkStatusPhase.Renewal,
            TrademarkStatusTone.Success),
        Item(
            "registro-extinto",
            "Registro extinto / Anotada a extinção",
            "A marca perdeu a validade por falta de renovação (caducidade de vigência), renúncia do titular ou declaração de nulidade.",
            TrademarkStatusPhase.Extinction,
            TrademarkStatusTone.Danger)
    ];

    private static int _order;

    private static Definition Item(
        string code,
        string title,
        string description,
        TrademarkStatusPhase phase,
        TrademarkStatusTone tone)
    {
        _order += 10;
        return new Definition(code, title, description, _order, phase, tone);
    }
}
