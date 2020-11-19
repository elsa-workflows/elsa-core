import React from "react";
import NavMenu from "/shared/NavMenu";
import Home from "/pages/Home";
import Workflows from "/pages/Workflows";
import {
    BrowserRouter as Router,
    Switch,
    Route,
    Link
} from "react-router-dom";

class App extends React.Component {
    render() {
        const {name} = this.props;
        return (
            <Router>
                <div>
                    <div style={{minHeight: '640px'}}>
                        <div className="h-screen flex overflow-hidden bg-white">
                            <NavMenu/>
                            <div className="flex flex-col w-0 flex-1 overflow-hidden">
                                <div className="relative z-10 flex-shrink-0 flex h-16 bg-white border-b border-gray-200 lg:hidden">
                                    <button className="px-4 border-r border-gray-200 text-gray-500 focus:outline-none focus:bg-gray-100 focus:text-gray-600 lg:hidden" aria-label="Open sidebar">
                                        <svg className="h-6 w-6" fill="none" strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" viewBox="0 0 24 24" stroke="currentColor">
                                            <path d="M4 6h16M4 12h8m-8 6h16"/>
                                        </svg>
                                    </button>
                                    <div className="flex-1 flex justify-between px-4 sm:px-6 lg:px-8">
                                        <div className="flex-1 flex">
                                            <form className="w-full flex md:ml-0" action="#" method="GET">
                                                <label htmlFor="search_field" className="sr-only">Search</label>
                                                <div className="relative w-full text-gray-400 focus-within:text-gray-600">
                                                    <div className="absolute inset-y-0 left-0 flex items-center pointer-events-none">
                                                        <svg className="h-5 w-5" fill="currentColor" viewBox="0 0 20 20">
                                                            <path fillRule="evenodd" clipRule="evenodd" d="M8 4a4 4 0 100 8 4 4 0 000-8zM2 8a6 6 0 1110.89 3.476l4.817 4.817a1 1 0 01-1.414 1.414l-4.816-4.816A6 6 0 012 8z"/>
                                                        </svg>
                                                    </div>
                                                    <input id="search_field" className="block w-full h-full pl-8 pr-3 py-2 rounded-md text-gray-900 placeholder-gray-500 focus:outline-none focus:placeholder-gray-400 sm:text-sm" placeholder="Search" type="search"/>
                                                </div>
                                            </form>
                                        </div>
                                    </div>
                                </div>
                                <main className="flex flex-col flex-1 relative z-0 overflow-y-auto focus:outline-none" tabIndex="0">
                                    <Switch>
                                        <Route exact path="/">
                                            <Home/>
                                        </Route>
                                        <Route path="/workflows">
                                            <Workflows/>
                                        </Route>
                                    </Switch>
                                </main>
                            </div>
                        </div>
                    </div>
                </div>
                <div style={{clear: 'both', display: 'block', height: 0}}/>
            </Router>
        );
    }
}

export default App;
