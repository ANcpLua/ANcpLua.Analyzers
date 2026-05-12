using ANcpLua.Analyzers.AnalyzerDocsGenerator;

await ProgramUtils.RunMainAsync<
    ANcpLua.Analyzers.AnalyzerDocs.AlAnalyzerDocsGenerator,
    ANcpLua.Analyzers.AnalyzerDocs.AlAnalyzerDocsVerifier>(args);
