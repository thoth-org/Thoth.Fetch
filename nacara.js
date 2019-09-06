const standard = require('nacara/dist/layouts/standard/Export').default;
const mdMessage = require('nacara/dist/js/utils').mdMessage;

module.exports = {
    githubURL: "https://github.com/thoth-org/Thoth.Fetch",
    url: "https://thoth-org.github.io/",
    source: "docs",
    output: "docs_deploy",
    baseUrl: "/Thoth.Fetch/",
    editUrl : "https://github.com/thoth-org/Thoth.Fetch/edit/master/docs",
    title: "Thoth.Fetch",
    debug: true,
    version: "1.1.0",
    navbar: {
        showVersion: true,
        links: [
            {
                href: "/Thoth.Fetch/index.html",
                label: "Documentation",
                icon: "fas fa-book"
            },
            {
                href: "/Thoth.Fetch/changelog.html",
                label: "Changelog",
                icon: "fas fa-tasks"
            },
            {
                href: "https://gitter.im/fable-compiler/Fable",
                label: "Support",
                icon: "fab fa-gitter",
                isExternal: true
            },
            {
                href: "https://github.com/thoth-org/Thoth.Fetch",
                icon: "fab fa-github",
                isExternal: true
            },
            {
                href: "https://twitter.com/MangelMaxime",
                icon: "fab fa-twitter",
                isExternal: true,
                color: "#55acee"
            }
        ]
    },
    lightner: {
        backgroundColor: "#FAFAFA",
        textColor: "",
        themeFile: "./paket-files/grammars/akamud/vscode-theme-onelight/themes/OneLight.json",
        grammars: [
            "./paket-files/grammars/ionide/ionide-fsgrammar/grammar/fsharp.json",
        ]
    },
    layouts: {
        default: standard.Default,
        changelog: standard.Changelog
    },
    plugins: {
        markdown: [
            {
                path: 'markdown-it-container',
                args: [
                    'warning',
                    mdMessage("warning")
                ]
            },
            {
                path: 'markdown-it-container',
                args: [
                    'info',
                    mdMessage("info")
                ]
            },
            {
                path: 'markdown-it-container',
                args: [
                    'success',
                    mdMessage("success")
                ]
            },
            {
                path: 'markdown-it-container',
                args: [
                    'danger',
                    mdMessage("danger")
                ]
            },
            {
                path: 'nacara/dist/js/markdown-it-anchored.js'
            },
            {
                path: 'nacara/dist/js/markdown-it-toc.js'
            }
        ]
    }
};
