hljs.registerLanguage('tsp-dsl', () => ({
    case_insensitive: true, // language is case-insensitive
    keywords: {
        keyword: 'and or xor andthen for wait',
        symbol: 'avg avgof max maxof min minof abs sin cos tan tg cot ctg'
    },
    contains: [
        {
            className: 'string',
            begin: '\'',
            end: '\''
        },
        {
            scope: 'variable',
            begin: '"',
            end: '"'
        },
        {
            scope: 'literal',
            match: /[0-9]+(\\.[0-9]+)?\s+(seconds|sec|minutes|min|hours|hr)/
        },
        {
            scope: 'number',
            match: /[0-9]+(\\.[0-9]+)?/
        }
    ]
}))