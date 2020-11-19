import React from "react";

export default function NavMenu() {
    return <>
        <div className="lg:hidden">
            <div className="fixed inset-0 flex z-40">
                <div className="fixed inset-0">
                    <div className="absolute inset-0 bg-gray-600 opacity-75"/>
                </div>
                <div>
                    <div className="absolute top-0 right-0 -mr-14 p-1">
                        <button className="flex items-center justify-center h-12 w-12 rounded-full focus:outline-none focus:bg-gray-600" aria-label="Close sidebar">
                            <svg className="h-6 w-6 text-white" stroke="currentColor" fill="none" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M6 18L18 6M6 6l12 12"/>
                            </svg>
                        </button>
                    </div>
                    <div className="flex-shrink-0 flex items-center px-4">
                        <img className="h-14 w-auto m-auto" src="../assets/logo.png" alt="Elsa"/>
                    </div>
                    <div className="mt-5 flex-1 h-0 overflow-y-auto">
                        <nav className="px-2">
                            <div className="space-y-1">
                                <a href="/" className="group flex items-center px-2 py-2 text-base leading-5 font-medium rounded-md text-gray-600 hover:text-gray-900 hover:bg-gray-50 focus:bg-gray-50 focus:outline-none transition ease-in-out duration-150">
                                    <svg className="mr-3 h-6 w-6 text-gray-500 group-hover:text-gray-500 group-focus:text-gray-600 transition ease-in-out duration-150" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6"/>
                                    </svg>
                                    Home
                                </a>
                            </div>
                        </nav>
                    </div>
                </div>
                <div className="flex-shrink-0 w-14">

                </div>
            </div>
        </div>

        <div className="hidden lg:flex lg:flex-shrink-0">
            <div className="flex flex-col w-64 border-r border-gray-200 pt-5 pb-4 bg-gray-100">
                <div className="flex items-center flex-shrink-0 px-6">
                    <img className="h-14 w-auto" src="../assets/logo.png" alt="Elsa"/>
                </div>

                <div className="h-0 flex-1 flex flex-col overflow-y-auto">

                    <div className="px-3 mt-5">
                        <label htmlFor="search" className="sr-only">Search</label>
                        <div className="mt-1 relative rounded-md shadow-sm">
                            <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                                <svg className="mr-3 h-4 w-4 text-gray-400" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                                    <path fillRule="evenodd" d="M8 4a4 4 0 100 8 4 4 0 000-8zM2 8a6 6 0 1110.89 3.476l4.817 4.817a1 1 0 01-1.414 1.414l-4.816-4.816A6 6 0 012 8z" clipRule="evenodd"/>
                                </svg>
                            </div>
                            <input id="search" className="form-input block w-full pl-9 sm:text-sm sm:leading-5" placeholder="Search"/>
                        </div>
                    </div>
                    <nav className="px-3 mt-6">
                        <div className="space-y-1">

                            <a href="/" className="group flex items-center px-2 py-2 text-sm leading-5 font-medium rounded-md text-gray-700 hover:text-gray-900 hover:bg-gray-50 focus:outline-none focus:bg-gray-50 transition ease-in-out duration-150">
                                <svg className="mr-3 h-6 w-6 text-gray-400 group-hover:text-gray-500 group-focus:text-gray-600 transition ease-in-out duration-150" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6"/>
                                </svg>
                                Home
                            </a>
                        </div>
                    </nav>
                </div>
            </div>
        </div>
    </>;
}